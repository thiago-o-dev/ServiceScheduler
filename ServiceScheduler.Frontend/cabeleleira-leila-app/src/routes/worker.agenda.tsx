import { createFileRoute } from "@tanstack/react-router";
import { useMutation, useQueryClient, useSuspenseQuery } from "@tanstack/react-query";
import { format } from "date-fns";
import { ptBR } from "date-fns/locale";
import { CheckCircle2, XCircle, CalendarHeart } from "lucide-react";
import { toast } from "sonner";
import { PageHeader } from "@/components/shell/PageHeader";
import { EmptyState } from "@/components/shell/EmptyState";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { schedulesApi } from "@/lib/api/schedules.api";
import { useAuth } from "@/lib/auth/auth-context";
import { brl } from "@/lib/format";
import { statusLabel, statusVariant } from "@/components/schedule/statusBadge";

export const Route = createFileRoute("/worker/agenda")({
  head: () => ({ meta: [{ title: "Agenda — Cabeleleira Leila" }] }),
  component: WorkerAgenda,
});

function WorkerAgenda() {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const q = useSuspenseQuery({
    queryKey: ["schedules", "worker", user?.workerId],
    queryFn: () => schedulesApi.list({ workerId: user?.workerId }),
  });

  const confirm = useMutation({
    mutationFn: (id: string) => schedulesApi.confirm(id),
    onSuccess: () => {
      toast.success("Agendamento confirmado.");
      queryClient.invalidateQueries({ queryKey: ["schedules"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    },
  });
  const cancel = useMutation({
    mutationFn: (id: string) => schedulesApi.cancel(id),
    onSuccess: () => {
      toast.success("Agendamento cancelado.");
      queryClient.invalidateQueries({ queryKey: ["schedules"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    },
  });

  const groups = new Map<string, typeof q.data>();
  for (const s of q.data) {
    const key = s.scheduledAt.slice(0, 10);
    if (!groups.has(key)) groups.set(key, []);
    groups.get(key)!.push(s);
  }

  return (
    <div className="space-y-8">
      <PageHeader title="Sua agenda" description="Seus próximos atendimentos." />

      {q.data.length === 0 ? (
        <EmptyState
          icon={<CalendarHeart className="h-6 w-6" />}
          title="Nenhum agendamento ainda"
          description="Quando uma cliente reservar com você, aparecerá aqui."
        />
      ) : (
        <div className="space-y-6">
          {[...groups.entries()].map(([day, items]) => (
            <section key={day} className="space-y-3">
              <h2 className="font-display text-lg text-muted-foreground">
                {format(new Date(day + "T00:00:00"), "EEEE, dd 'de' MMMM", { locale: ptBR })}
              </h2>
              <ul className="grid gap-3">
                {items.map((s) => (
                  <Card key={s.id}>
                    <CardContent className="flex flex-col gap-3 p-5 sm:flex-row sm:items-center sm:justify-between">
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="font-display text-xl text-primary">
                            {format(new Date(s.scheduledAt), "HH:mm")}
                          </span>
                          <span className="font-medium">{s.customerName}</span>
                          <Badge variant={statusVariant(s.status)}>{statusLabel(s.status)}</Badge>
                        </div>
                        <div className="mt-1 text-sm text-muted-foreground">
                          {s.services.map((x) => x.name).join(", ")} · {brl(s.netValue)}
                        </div>
                      </div>
                      <div className="flex gap-2">
                        {s.status === "Pending" && (
                          <Button size="sm" onClick={() => confirm.mutate(s.id)}>
                            <CheckCircle2 className="h-4 w-4" /> Confirmar
                          </Button>
                        )}
                        {s.status !== "Cancelled" && (
                          <Button size="sm" variant="outline" onClick={() => cancel.mutate(s.id)}>
                            <XCircle className="h-4 w-4" /> Cancelar
                          </Button>
                        )}
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </ul>
            </section>
          ))}
        </div>
      )}
    </div>
  );
}
