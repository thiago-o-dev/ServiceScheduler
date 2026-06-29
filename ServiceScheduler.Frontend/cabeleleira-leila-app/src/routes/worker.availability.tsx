import { createFileRoute } from "@tanstack/react-router";
import { useMutation, useQueryClient, useSuspenseQuery } from "@tanstack/react-query";
import { useState } from "react";
import { format } from "date-fns";
import { Trash2, Plus } from "lucide-react";
import { toast } from "sonner";
import { PageHeader } from "@/components/shell/PageHeader";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from "@/components/ui/select";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import { workersApi } from "@/lib/api/workers.api";
import { useAuth } from "@/lib/auth/auth-context";
import { DayOfWeek } from "@/lib/api/types";
import { dayName } from "@/lib/format";

export const Route = createFileRoute("/worker/availability")({
  head: () => ({ meta: [{ title: "Disponibilidade — Cabeleleira Leila" }] }),
  component: AvailabilityPage,
});

function AvailabilityPage() {
  const { user } = useAuth();
  const workerId = user?.workerId!;
  return (
    <div className="space-y-8">
      <PageHeader title="Disponibilidade" description="Defina seus horários semanais e folgas." />
      <Tabs defaultValue="weekly">
        <TabsList>
          <TabsTrigger value="weekly">Horários semanais</TabsTrigger>
          <TabsTrigger value="unavailable">Folgas & ausências</TabsTrigger>
        </TabsList>
        <TabsContent value="weekly">
          <WeeklyEditor workerId={workerId} />
        </TabsContent>
        <TabsContent value="unavailable">
          <UnavailableEditor workerId={workerId} />
        </TabsContent>
      </Tabs>
    </div>
  );
}

function WeeklyEditor({ workerId }: { workerId: string }) {
  const queryClient = useQueryClient();
  const q = useSuspenseQuery({
    queryKey: ["available-periods", workerId],
    queryFn: () => workersApi.listAvailable(workerId),
  });
  const [dow, setDow] = useState(String(DayOfWeek.Monday));
  const [start, setStart] = useState("09:00");
  const [end, setEnd] = useState("18:00");

  const add = useMutation({
    mutationFn: () =>
      workersApi.addAvailable(workerId, { dayOfWeek: Number(dow), startTime: start, endTime: end }),
    onSuccess: () => {
      toast.success("Período adicionado.");
      queryClient.invalidateQueries({ queryKey: ["available-periods", workerId] });
    },
  });
  const remove = useMutation({
    mutationFn: (id: string) => workersApi.removeAvailable(workerId, { id }),
    onSuccess: () => {
      toast.success("Período removido.");
      queryClient.invalidateQueries({ queryKey: ["available-periods", workerId] });
    },
  });

  return (
    <div className="mt-4 grid gap-4 lg:grid-cols-[320px_1fr]">
      <Card>
        <CardContent className="space-y-3 p-5">
          <div className="font-display text-lg">Adicionar período</div>
          <div className="space-y-2">
            <Label>Dia da semana</Label>
            <Select value={dow} onValueChange={setDow}>
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                {Array.from({ length: 7 }).map((_, i) => (
                  <SelectItem key={i} value={String(i)}>{dayName(i)}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="grid grid-cols-2 gap-2">
            <div className="space-y-2">
              <Label>Início</Label>
              <Input type="time" value={start} onChange={(e) => setStart(e.target.value)} />
            </div>
            <div className="space-y-2">
              <Label>Fim</Label>
              <Input type="time" value={end} onChange={(e) => setEnd(e.target.value)} />
            </div>
          </div>
          <Button className="w-full" onClick={() => add.mutate()} disabled={add.isPending}>
            <Plus className="h-4 w-4" /> Adicionar
          </Button>
        </CardContent>
      </Card>

      <div className="space-y-2">
        {Array.from({ length: 7 }).map((_, i) => {
          const periods = q.data.filter((p) => p.dayOfWeek === i);
          return (
            <Card key={i}>
              <CardContent className="flex items-center justify-between gap-3 p-4">
                <div className="w-32 font-medium">{dayName(i)}</div>
                <div className="flex flex-1 flex-wrap gap-2">
                  {periods.length === 0 ? (
                    <span className="text-sm text-muted-foreground">Folga</span>
                  ) : (
                    periods.map((p) => (
                      <span key={p.id} className="flex items-center gap-2 rounded-full bg-secondary px-3 py-1 text-sm">
                        {p.startTime} – {p.endTime}
                        <button onClick={() => remove.mutate(p.id)} aria-label="Remover">
                          <Trash2 className="h-3 w-3 text-muted-foreground hover:text-destructive" />
                        </button>
                      </span>
                    ))
                  )}
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>
    </div>
  );
}

function UnavailableEditor({ workerId }: { workerId: string }) {
  const queryClient = useQueryClient();
  const [start, setStart] = useState(format(new Date(), "yyyy-MM-dd'T'09:00"));
  const [end, setEnd] = useState(format(new Date(), "yyyy-MM-dd'T'18:00"));
  const [reason, setReason] = useState("");
  const [preempt, setPreempt] = useState(false);

  const add = useMutation({
    mutationFn: () => {
      const body = { start: new Date(start).toISOString(), end: new Date(end).toISOString(), reason };
      return preempt
        ? workersApi.preemptUnavailable(workerId, body)
        : workersApi.addUnavailable(workerId, body);
    },
    onSuccess: () => {
      toast.success(preempt ? "Ausência registrada e horários cancelados." : "Ausência registrada.");
      queryClient.invalidateQueries({ queryKey: ["schedules"] });
    },
  });

  return (
    <div className="mt-4 max-w-xl">
      <Card>
        <CardContent className="space-y-3 p-5">
          <div className="font-display text-lg">Registrar ausência</div>
          <p className="text-sm text-muted-foreground">
            Use <strong>Forçar</strong> para cancelar automaticamente agendamentos que conflitam.
          </p>
          <div className="grid gap-3 sm:grid-cols-2">
            <div className="space-y-2">
              <Label>Início</Label>
              <Input type="datetime-local" value={start} onChange={(e) => setStart(e.target.value)} />
            </div>
            <div className="space-y-2">
              <Label>Fim</Label>
              <Input type="datetime-local" value={end} onChange={(e) => setEnd(e.target.value)} />
            </div>
          </div>
          <div className="space-y-2">
            <Label>Motivo</Label>
            <Input value={reason} onChange={(e) => setReason(e.target.value)} placeholder="Ex: férias, atestado..." />
          </div>
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={preempt} onChange={(e) => setPreempt(e.target.checked)} />
            Cancelar agendamentos em conflito (forçar)
          </label>
          <Button onClick={() => add.mutate()} disabled={add.isPending}>
            {add.isPending ? "Salvando..." : "Salvar ausência"}
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}
