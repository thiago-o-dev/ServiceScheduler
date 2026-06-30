import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient, useSuspenseQuery } from "@tanstack/react-query";
import { addDays, addMinutes, format, isBefore, isSameDay } from "date-fns";
import { ptBR } from "date-fns/locale";
import { Check, Clock, Scissors, Users } from "lucide-react";
import { toast } from "sonner";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { servicesApi } from "@/lib/api/services.api";
import { bundlesApi } from "@/lib/api/serviceBundles.api";
import { workersApi } from "@/lib/api/workers.api";
import { schedulesApi } from "@/lib/api/schedules.api";
import { useAuth } from "@/lib/auth/auth-context";
import { brl } from "@/lib/format";
import { minutesToTimeSpan, timeSpanToMinutes, formatDurationLabel } from "@/lib/time/duration";
import { cn } from "@/lib/utils";
import type { UUID } from "@/lib/api/types";

const servicesQ = { queryKey: ["services"], queryFn: () => servicesApi.list() };
const bundlesQ = { queryKey: ["bundles"], queryFn: () => bundlesApi.list() };
const workersQ = { queryKey: ["workers"], queryFn: () => workersApi.list() };

const SLOT_STEP_MINUTES = 30;

/**
 * The availability endpoint now returns a map of workerId -> the open time
 * ranges that worker has free on the requested day, e.g.:
 * {
 *   "7b6cb74c-02af-4d99-8c03-5c3f801fb201": [
 *     { "start": "2026-06-30T09:00:00", "end": "2026-06-30T11:00:00" },
 *     { "start": "2026-06-30T13:00:00", "end": "2026-06-30T18:00:00" }
 *   ]
 * }
 * NOTE: update servicesApi.availableHours to call the new endpoint with
 * (serviceIds: UUID[], date: string) and return this shape.
 */
interface TimeInterval {
  start: string;
  end: string;
}
type WorkerAvailabilityMap = Record<UUID, TimeInterval[]>;

interface SlotOption {
  start: Date;
  /** every worker who can cover the full appointment, starting at this slot */
  workerIds: UUID[];
}

interface ServiceBundle {
  id: UUID;
  name: string;
  description: string;
  serviceIds: UUID[];
  value: number;
  discount: number;
}

interface BundleMatch {
  bundle: ServiceBundle;
  /** sum of the individual service prices that make up this bundle */
  naiveValue: number;
  /** naiveValue - bundle.value, i.e. how much the customer saves */
  savings: number;
}

function floorToStep(date: Date, stepMinutes: number) {
  const d = new Date(date);
  d.setSeconds(0, 0);
  d.setMinutes(d.getMinutes() - (d.getMinutes() % stepMinutes));
  return d;
}

/**
 * Builds the 30-minute slot grid for a day. A block is kept only if at
 * least one worker has a single open interval that covers the entire
 * appointment duration (totalMinutes) starting exactly at that block.
 */
function buildSlotOptions(availability: WorkerAvailabilityMap, totalMinutes: number): SlotOption[] {
  const ranges = Object.entries(availability).flatMap(([workerId, intervals]) =>
    intervals.map((i) => ({ workerId, start: new Date(i.start), end: new Date(i.end) })),
  );
  if (ranges.length === 0 || totalMinutes <= 0) return [];

  const earliest = ranges.reduce((min, r) => (r.start < min ? r.start : min), ranges[0].start);
  const latest = ranges.reduce((max, r) => (r.end > max ? r.end : max), ranges[0].end);

  const now = new Date();
  const options: SlotOption[] = [];

  for (
    let cursor = floorToStep(earliest, SLOT_STEP_MINUTES);
    addMinutes(cursor, totalMinutes) <= latest;
    cursor = addMinutes(cursor, SLOT_STEP_MINUTES)
  ) {
    if (isBefore(cursor, now)) continue;

    const cursorEnd = addMinutes(cursor, totalMinutes);
    const workerIds = Array.from(
      new Set(
        ranges
          .filter((r) => r.start <= cursor && cursorEnd <= r.end)
          .map((r) => r.workerId),
      ),
    );
    if (workerIds.length > 0) {
      options.push({ start: new Date(cursor), workerIds });
    }
  }
  return options;
}

/**
 * Given the full bundle list and the customer's current service selection,
 * finds the set of non-overlapping bundles (every bundle service must be
 * selected, and no two active bundles may share a service) that maximizes
 * total savings. Anything left over after the chosen bundles is priced
 * individually.
 *
 * This is a brute-force search over "qualifying" candidates only (bundles
 * fully covered by the current selection), so in practice the search space
 * is tiny — most salons won't have more than a handful of bundles
 * simultaneously satisfied by one selection.
 */
function computeActiveBundles(
  bundles: ServiceBundle[],
  selectedServices: UUID[],
  services: { id: UUID; value: number }[],
): { active: BundleMatch[]; leftoverServiceIds: UUID[] } {
  const selectedSet = new Set(selectedServices);

  const candidates: BundleMatch[] = bundles
    .filter((b) => b.serviceIds.length > 0 && b.serviceIds.every((id) => selectedSet.has(id)))
    .map((b) => {
      const naiveValue = b.serviceIds.reduce(
        (sum, id) => sum + (services.find((s) => s.id === id)?.value ?? 0),
        0,
      );
      return { bundle: b, naiveValue, savings: naiveValue - b.value };
    })
    // never auto-apply a "bundle" that isn't actually cheaper than buying separately
    .filter((c) => c.savings > 0);

  if (candidates.length === 0) {
    return { active: [], leftoverServiceIds: selectedServices };
  }

  let best: { combo: BundleMatch[]; savings: number } = { combo: [], savings: 0 };

  function backtrack(i: number, chosen: BundleMatch[], used: Set<UUID>, savings: number) {
    if (savings > best.savings) best = { combo: chosen, savings };
    if (i === candidates.length) return;
    backtrack(i + 1, chosen, used, savings); // skip candidate i
    const c = candidates[i];
    const overlaps = c.bundle.serviceIds.some((id) => used.has(id));
    if (!overlaps) {
      const nextUsed = new Set(used);
      c.bundle.serviceIds.forEach((id) => nextUsed.add(id));
      backtrack(i + 1, [...chosen, c], nextUsed, savings + c.savings);
    }
  }
  backtrack(0, [], new Set(), 0);

  const usedIds = new Set<UUID>();
  best.combo.forEach((c) => c.bundle.serviceIds.forEach((id) => usedIds.add(id)));
  const leftoverServiceIds = selectedServices.filter((id) => !usedIds.has(id));

  return { active: best.combo, leftoverServiceIds };
}

export function BookingWizard() {
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const customerId = user?.customerId;

  const { data: services } = useSuspenseQuery(servicesQ);
  const { data: bundles } = useSuspenseQuery(bundlesQ);
  const { data: workers } = useSuspenseQuery(workersQ);

  const [selectedServices, setSelectedServices] = useState<UUID[]>([]);
  const [preferredWorkerId, setPreferredWorkerId] = useState<UUID | null>(null);
  const [date, setDate] = useState<Date>(() => addDays(new Date(), 1));
  const [slot, setSlot] = useState<string | null>(null);
  const [assignedWorkerId, setAssignedWorkerId] = useState<UUID | null>(null);

  const totalMinutes = useMemo(
    () =>
      selectedServices.reduce((sum, id) => {
        const s = services.find((x) => x.id === id);
        return sum + timeSpanToMinutes(s?.duration ?? "01:00:00");
      }, 0),
    [selectedServices, services],
  );

  // un-discounted sum of every selected service — used only for the
  // strikethrough "preço sem pacotes" line in the summary
  const totalValue = useMemo(
    () => selectedServices.reduce((sum, id) => sum + (services.find((s) => s.id === id)?.value ?? 0), 0),
    [selectedServices, services],
  );

  // bundle packing: figures out which bundles are simultaneously active
  // and what's left to be priced at full price
  const { active: activeBundles, leftoverServiceIds } = useMemo(
    () => computeActiveBundles(bundles, selectedServices, services),
    [bundles, selectedServices, services],
  );

  const activeBundleIds = useMemo(
    () => new Set(activeBundles.map((b) => b.bundle.id)),
    [activeBundles],
  );

  const leftoverValue = useMemo(
    () => leftoverServiceIds.reduce((sum, id) => sum + (services.find((s) => s.id === id)?.value ?? 0), 0),
    [leftoverServiceIds, services],
  );

  const bundledValue = activeBundles.reduce((sum, b) => sum + b.bundle.value, 0);
  const totalSavings = activeBundles.reduce((sum, b) => sum + b.savings, 0);
  const displayTotal = leftoverValue + bundledValue; // what the customer actually pays

  const dateKey = format(date, "yyyy-MM-dd");

  const availabilityQ = useQuery({
    queryKey: ["worker-availability", selectedServices, dateKey],
    queryFn: () => servicesApi.availableHours(selectedServices, dateKey) as Promise<WorkerAvailabilityMap>,
    enabled: selectedServices.length > 0,
  });

  const slotOptions = useMemo(
    () => buildSlotOptions(availabilityQ.data ?? {}, totalMinutes),
    [availabilityQ.data, totalMinutes],
  );

  // only offer professionals who actually have an entry in today's availability map
  const availableWorkerIds = useMemo(
    () => new Set(Object.keys(availabilityQ.data ?? {})),
    [availabilityQ.data],
  );

  const visibleSlots = useMemo(
    () => (preferredWorkerId ? slotOptions.filter((s) => s.workerIds.includes(preferredWorkerId)) : slotOptions),
    [slotOptions, preferredWorkerId],
  );

  const create = useMutation({
    mutationFn: () =>
      schedulesApi.create({
        customerId: customerId!,
        workerId: assignedWorkerId!,
        serviceIds: selectedServices,
        scheduledAt: slot!,
        duration: minutesToTimeSpan(totalMinutes),
      }),
    onSuccess: () => {
      toast.success("Horário reservado! Aguarde a confirmação.");
      queryClient.invalidateQueries({ queryKey: ["schedules"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      setSelectedServices([]);
      setSlot(null);
      setAssignedWorkerId(null);
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const resetSelection = () => {
    setSlot(null);
    setAssignedWorkerId(null);
  };

  const toggleService = (id: UUID) => {
    resetSelection();
    setSelectedServices((prev) => (prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]));
  };
  const applyBundle = (bundleId: UUID) => {
    const b = bundles.find((x) => x.id === bundleId);
    if (b) {
      resetSelection();
      setSelectedServices((prev) => Array.from(new Set([...prev, ...b.serviceIds])));
    }
  };

  const pickSlot = (option: SlotOption) => {
    setSlot(option.start.toISOString());
    setAssignedWorkerId(preferredWorkerId ?? option.workerIds[0]);
  };

  const days = Array.from({ length: 14 }, (_, i) => addDays(new Date(), i));
  const ready = selectedServices.length > 0 && slot && assignedWorkerId && customerId;
  const assignedWorker = workers.find((w) => w.id === assignedWorkerId);

  return (
    <div className="grid gap-6 lg:grid-cols-[1fr_360px]">
      <div className="min-w-0 space-y-6">
        <Section step="1" title="Escolha os serviços">
          {bundles.length > 0 && (
            <div className="mb-4 flex flex-wrap gap-2">
              {bundles.map((b) => {
                const active = activeBundleIds.has(b.id);
                return (
                  <button
                    key={b.id}
                    onClick={() => applyBundle(b.id)}
                    className={cn(
                      "rounded-full border border-dashed px-3 py-1 text-xs font-medium transition",
                      active
                        ? "border-primary bg-primary/15 text-primary"
                        : "border-primary/40 bg-secondary text-primary hover:bg-primary/10",
                    )}
                  >
                    ✨ {b.name} · -{b.discount}%
                  </button>
                );
              })}
            </div>
          )}
          <div className="grid gap-3 sm:grid-cols-2">
            {services.map((s) => {
              const active = selectedServices.includes(s.id);
              return (
                <button
                  key={s.id}
                  onClick={() => toggleService(s.id)}
                  className={cn(
                    "group flex items-start justify-between gap-3 rounded-xl border bg-card p-4 text-left transition",
                    active ? "border-primary ring-2 ring-primary/30" : "hover:border-primary/40",
                  )}
                >
                  <div>
                    <div className="font-medium">{s.name}</div>
                    <p className="mt-1 text-xs text-muted-foreground">{s.description}</p>
                    <div className="mt-2 flex items-center gap-2 text-xs text-muted-foreground">
                      <Clock className="h-3 w-3" /> {formatDurationLabel(timeSpanToMinutes(s.duration ?? "01:00:00"))}
                    </div>
                  </div>
                  <div className="text-right">
                    <div className="font-display text-lg text-primary">{brl(s.value)}</div>
                    {active && <Check className="ml-auto mt-1 h-4 w-4 text-primary" />}
                  </div>
                </button>
              );
            })}
          </div>
        </Section>

        <Section step="2" title="Quando?" disabled={selectedServices.length === 0}>
          <div className="-mx-1 mb-4 flex gap-2 overflow-x-auto px-1 pb-1">
            {days.map((d) => {
              const active = isSameDay(d, date);
              return (
                <button
                  key={d.toISOString()}
                  onClick={() => {
                    setDate(d);
                    resetSelection();
                  }}
                  className={cn(
                    "flex min-w-[68px] flex-col items-center rounded-lg border px-3 py-2 text-xs",
                    active ? "border-primary bg-primary text-primary-foreground" : "border-border bg-card hover:border-primary/40",
                  )}
                >
                  <span className="uppercase tracking-wider opacity-80">{format(d, "EEE", { locale: ptBR })}</span>
                  <span className="mt-1 font-display text-xl">{format(d, "dd")}</span>
                  <span className="opacity-80">{format(d, "MMM", { locale: ptBR })}</span>
                </button>
              );
            })}
          </div>

          {workers.length > 1 && (
            <div className="mb-5">
              <p className="mb-3 text-sm font-medium text-muted-foreground">Profissional</p>
              <div className="grid gap-3 sm:grid-cols-3">
                <button
                  onClick={() => {
                    setPreferredWorkerId(null);
                    resetSelection();
                  }}
                  className={cn(
                    "rounded-xl border bg-card p-4 text-left transition",
                    preferredWorkerId === null ? "border-primary ring-2 ring-primary/30" : "hover:border-primary/40",
                  )}
                >
                  <div className="grid h-14 w-14 place-items-center rounded-full bg-secondary text-primary">
                    <Users className="h-6 w-6" />
                  </div>
                  <div className="mt-3 font-medium">Qualquer profissional</div>
                  <div className="text-xs text-muted-foreground">Atribuição automática</div>
                </button>
                {workers.map((w) => {
                  const active = preferredWorkerId === w.id;
                  const hasAvailability = availableWorkerIds.has(w.id);
                  return (
                    <button
                      key={w.id}
                      disabled={!hasAvailability}
                      onClick={() => {
                        setPreferredWorkerId(w.id);
                        resetSelection();
                      }}
                      className={cn(
                        "rounded-xl border bg-card p-4 text-left transition",
                        active ? "border-primary ring-2 ring-primary/30" : "hover:border-primary/40",
                        !hasAvailability && "cursor-not-allowed opacity-40",
                      )}
                    >
                      <div className="grid h-14 w-14 place-items-center rounded-full bg-secondary font-display text-xl text-primary">
                        {w.name.charAt(0)}
                      </div>
                      <div className="mt-3 font-medium">{w.name}</div>
                      <div className="text-xs text-muted-foreground">
                        {hasAvailability ? w.title ?? "Profissional" : "Sem horários neste dia"}
                      </div>
                    </button>
                  );
                })}
              </div>
            </div>
          )}

          {availabilityQ.isLoading ? (
            <div className="grid grid-cols-3 gap-2 sm:grid-cols-5">
              {Array.from({ length: 10 }).map((_, i) => (
                <Skeleton key={i} className="h-10" />
              ))}
            </div>
          ) : visibleSlots.length === 0 ? (
            <div className="rounded-lg border border-dashed p-6 text-center text-sm text-muted-foreground">
              Nenhum horário disponível neste dia. Tente outra data{preferredWorkerId ? " ou profissional" : ""}.
            </div>
          ) : (
            <div className="grid grid-cols-3 gap-2 sm:grid-cols-5">
              {visibleSlots.map((option) => {
                const iso = option.start.toISOString();
                const active = slot === iso;
                return (
                  <button
                    key={iso}
                    onClick={() => pickSlot(option)}
                    className={cn(
                      "rounded-lg border px-3 py-2 text-sm font-medium",
                      active ? "border-primary bg-primary text-primary-foreground" : "border-border bg-card hover:border-primary/40",
                    )}
                  >
                    {format(option.start, "HH:mm")}
                  </button>
                );
              })}
            </div>
          )}
        </Section>
      </div>

      <aside className="min-w-0 lg:sticky lg:top-24 lg:self-start">
        <Card>
          <CardContent className="space-y-4 p-6">
            <div className="flex items-center gap-2 font-display text-lg">
              <Scissors className="h-4 w-4 text-primary" /> Resumo do agendamento
            </div>
            {selectedServices.length === 0 ? (
              <p className="text-sm text-muted-foreground">Selecione ao menos um serviço para começar.</p>
            ) : (
              <>
                <ul className="space-y-2">
                  {selectedServices.map((id) => {
                    const s = services.find((x) => x.id === id);
                    return s ? (
                      <li key={id} className="flex items-center justify-between text-sm">
                        <span>{s.name}</span>
                        <span className="font-medium">{brl(s.value)}</span>
                      </li>
                    ) : null;
                  })}
                </ul>

                {activeBundles.length > 0 && (
                  <div className="space-y-2">
                    {activeBundles.map(({ bundle, savings }) => (
                      <div
                        key={bundle.id}
                        className="flex items-center justify-between gap-2 rounded-lg border border-dashed border-primary/40 bg-primary/5 px-3 py-2 text-xs font-medium text-primary"
                      >
                        <span>✨ {bundle.name} · -{bundle.discount}%</span>
                        <span>economiza {brl(savings)}</span>
                      </div>
                    ))}
                  </div>
                )}

                <div className="space-y-1 border-t pt-3 text-sm">
                  <div className="flex justify-between text-muted-foreground">
                    <span>Duração</span>
                    <span>{formatDurationLabel(totalMinutes)}</span>
                  </div>
                  {totalSavings > 0 && (
                    <div className="flex justify-between text-muted-foreground">
                      <span>Preço sem pacotes</span>
                      <span className="line-through">{brl(totalValue)}</span>
                    </div>
                  )}
                  <div className="flex justify-between font-display text-lg">
                    <span>Total</span>
                    <span className="text-primary">{brl(displayTotal)}</span>
                  </div>
                </div>
                {assignedWorker && (
                  <Badge variant="secondary">
                    Com {assignedWorker.name}
                    {!preferredWorkerId && " · atribuído automaticamente"}
                  </Badge>
                )}
                {slot && (
                  <div className="rounded-md bg-secondary p-3 text-sm">
                    {format(new Date(slot), "EEEE, dd 'de' MMMM 'às' HH:mm", { locale: ptBR })}
                  </div>
                )}
              </>
            )}
            <Button className="w-full" size="lg" disabled={!ready || create.isPending} onClick={() => create.mutate()}>
              {create.isPending ? "Reservando..." : "Confirmar reserva"}
            </Button>
          </CardContent>
        </Card>
      </aside>
    </div>
  );
}

function Section({
  step,
  title,
  children,
  disabled,
}: {
  step: string;
  title: string;
  children: React.ReactNode;
  disabled?: boolean;
}) {
  return (
    <section className={cn("rounded-2xl border bg-card p-6", disabled && "opacity-50 pointer-events-none")}>
      <div className="mb-4 flex items-center gap-3">
        <span className="grid h-8 w-8 place-items-center rounded-full bg-primary font-display text-primary-foreground">
          {step}
        </span>
        <h2 className="font-display text-xl">{title}</h2>
      </div>
      {children}
    </section>
  );
}