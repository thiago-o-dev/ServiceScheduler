import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient, useSuspenseQuery } from "@tanstack/react-query";
import { addDays, format, isSameDay } from "date-fns";
import { ptBR } from "date-fns/locale";
import { Check, Clock, Scissors } from "lucide-react";
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

export function BookingWizard() {
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const customerId = user?.customerId;

  const { data: services } = useSuspenseQuery(servicesQ);
  const { data: bundles } = useSuspenseQuery(bundlesQ);
  const { data: workers } = useSuspenseQuery(workersQ);

  const [selectedServices, setSelectedServices] = useState<UUID[]>([]);
  const [workerId, setWorkerId] = useState<UUID | null>(null);
  const [date, setDate] = useState<Date>(() => addDays(new Date(), 1));
  const [slot, setSlot] = useState<string | null>(null);

  const totalMinutes = useMemo(
    () =>
      selectedServices.reduce((sum, id) => {
        const s = services.find((x) => x.id === id);
        return sum + timeSpanToMinutes(s?.duration ?? "01:00:00");
      }, 0),
    [selectedServices, services],
  );
  const totalValue = useMemo(
    () => selectedServices.reduce((sum, id) => sum + (services.find((s) => s.id === id)?.value ?? 0), 0),
    [selectedServices, services],
  );

  const primaryServiceId = selectedServices[0];
  const dateKey = format(date, "yyyy-MM-dd");
  const slotsQ = useQuery({
    queryKey: ["available-hours", primaryServiceId, dateKey, workerId],
    queryFn: () => servicesApi.availableHours(primaryServiceId!, dateKey, workerId ?? undefined),
    enabled: !!primaryServiceId && !!workerId,
  });

  const create = useMutation({
    mutationFn: () =>
      schedulesApi.create({
        customerId: customerId!,
        workerId: workerId!,
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
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const toggleService = (id: UUID) => {
    setSlot(null);
    setSelectedServices((prev) =>
      prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id],
    );
  };
  const applyBundle = (bundleId: UUID) => {
    const b = bundles.find((x) => x.id === bundleId);
    if (b) {
      setSlot(null);
      setSelectedServices(b.serviceIds);
    }
  };

  const days = Array.from({ length: 14 }, (_, i) => addDays(new Date(), i));
  const ready = selectedServices.length > 0 && workerId && slot && customerId;

  return (
    <div className="grid gap-6 lg:grid-cols-[1fr_360px]">
      <div className="space-y-6">
        <Section step="1" title="Escolha os serviços">
          {bundles.length > 0 && (
            <div className="mb-4 flex flex-wrap gap-2">
              {bundles.map((b) => (
                <button
                  key={b.id}
                  onClick={() => applyBundle(b.id)}
                  className="rounded-full border border-dashed border-primary/40 bg-secondary px-3 py-1 text-xs font-medium text-primary transition hover:bg-primary/10"
                >
                  ✨ {b.name} · -{b.discount}%
                </button>
              ))}
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

        <Section step="2" title="Quem fará seu atendimento?" disabled={selectedServices.length === 0}>
          <div className="grid gap-3 sm:grid-cols-3">
            {workers.map((w) => {
              const active = workerId === w.id;
              return (
                <button
                  key={w.id}
                  onClick={() => {
                    setWorkerId(w.id);
                    setSlot(null);
                  }}
                  className={cn(
                    "rounded-xl border bg-card p-4 text-left transition",
                    active ? "border-primary ring-2 ring-primary/30" : "hover:border-primary/40",
                  )}
                >
                  <div className="grid h-14 w-14 place-items-center rounded-full bg-secondary font-display text-xl text-primary">
                    {w.name.charAt(0)}
                  </div>
                  <div className="mt-3 font-medium">{w.name}</div>
                  <div className="text-xs text-muted-foreground">{w.title ?? "Profissional"}</div>
                </button>
              );
            })}
          </div>
        </Section>

        <Section step="3" title="Quando?" disabled={!workerId}>
          <div className="-mx-1 mb-4 flex gap-2 overflow-x-auto px-1 pb-1">
            {days.map((d) => {
              const active = isSameDay(d, date);
              return (
                <button
                  key={d.toISOString()}
                  onClick={() => {
                    setDate(d);
                    setSlot(null);
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

          {slotsQ.isLoading ? (
            <div className="grid grid-cols-4 gap-2 sm:grid-cols-6">
              {Array.from({ length: 8 }).map((_, i) => <Skeleton key={i} className="h-10" />)}
            </div>
          ) : !slotsQ.data || slotsQ.data.slots.length === 0 ? (
            <div className="rounded-lg border border-dashed p-6 text-center text-sm text-muted-foreground">
              Nenhum horário disponível neste dia. Tente outra data.
            </div>
          ) : (
            <div className="grid grid-cols-3 gap-2 sm:grid-cols-5">
              {slotsQ.data.slots.map((s) => {
                const active = slot === s.start;
                return (
                  <button
                    key={s.start}
                    onClick={() => setSlot(s.start)}
                    className={cn(
                      "rounded-lg border px-3 py-2 text-sm font-medium",
                      active ? "border-primary bg-primary text-primary-foreground" : "border-border bg-card hover:border-primary/40",
                    )}
                  >
                    {format(new Date(s.start), "HH:mm")}
                  </button>
                );
              })}
            </div>
          )}
        </Section>
      </div>

      <aside className="lg:sticky lg:top-24 lg:self-start">
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
                <div className="space-y-1 border-t pt-3 text-sm">
                  <div className="flex justify-between text-muted-foreground">
                    <span>Duração</span>
                    <span>{formatDurationLabel(totalMinutes)}</span>
                  </div>
                  <div className="flex justify-between font-display text-lg">
                    <span>Total</span>
                    <span className="text-primary">{brl(totalValue)}</span>
                  </div>
                </div>
                {workerId && (
                  <Badge variant="secondary">
                    Com {workers.find((w) => w.id === workerId)?.name}
                  </Badge>
                )}
                {slot && (
                  <div className="rounded-md bg-secondary p-3 text-sm">
                    {format(new Date(slot), "EEEE, dd 'de' MMMM 'às' HH:mm", { locale: ptBR })}
                  </div>
                )}
              </>
            )}
            <Button
              className="w-full"
              size="lg"
              disabled={!ready || create.isPending}
              onClick={() => create.mutate()}
            >
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
