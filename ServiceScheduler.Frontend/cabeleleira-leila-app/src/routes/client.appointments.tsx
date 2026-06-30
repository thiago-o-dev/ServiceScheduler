import { createFileRoute } from "@tanstack/react-router";
import { useMutation, useQueryClient, useSuspenseQuery } from "@tanstack/react-query";
import { format } from "date-fns";
import { ptBR } from "date-fns/locale";
import { toast } from "sonner";
import { PageHeader } from "@/components/shell/PageHeader";
import { EmptyState } from "@/components/shell/EmptyState";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { CalendarHeart } from "lucide-react";
import { schedulesApi } from "@/lib/api/schedules.api";
import { brl } from "@/lib/format";
import { statusLabel, statusVariant } from "@/components/schedule/statusBadge";

const schedulesQ = { queryKey: ["schedules", "client"], queryFn: () => schedulesApi.list() };

export const Route = createFileRoute("/client/appointments")({
  head: () => ({ meta: [{ title: "Meus horários — Cabeleleira Leila" }] }),
  loader: ({ context }) => context.queryClient.ensureQueryData(schedulesQ),
  component: Appointments,
});

function Appointments() {
  const { data } = useSuspenseQuery(schedulesQ);
  const queryClient = useQueryClient();
  const cancel = useMutation({
    mutationFn: (id: string) => schedulesApi.cancel(id),
    onSuccess: () => {
      toast.success("Reserva cancelada.");
      queryClient.invalidateQueries({ queryKey: ["schedules"] });
    },
    onError: (error) => {
      toast.error((error as Error).message || "Não foi possível cancelar.");
    },
  });

  const now = Date.now();
  const upcoming = data.filter(
    (s) => new Date(s.scheduledAt).getTime() >= now && s.status !== "Cancelled",
  );
  const past = data.filter(
    (s) => new Date(s.scheduledAt).getTime() < now || s.status === "Cancelled",
  );

  return (
    <div className="space-y-8">
      <PageHeader title="Meus horários" description="Acompanhe e gerencie seus agendamentos." />

      <section>
        <h2 className="mb-3 font-display text-xl">Próximos</h2>
        {upcoming.length === 0 ? (
          <EmptyState
            icon={<CalendarHeart className="h-6 w-6" />}
            title="Nenhum horário agendado"
            description="Que tal reservar seu próximo cuidado?"
          />
        ) : (
          <ul className="grid gap-3">
            {upcoming.map((s) => (
              <Card key={s.id}>
                <CardContent className="flex flex-col gap-3 p-5 sm:flex-row sm:items-center sm:justify-between">
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="font-display text-lg">
                        {format(new Date(s.scheduledAt), "EEEE, dd 'de' MMM 'às' HH:mm", {
                          locale: ptBR,
                        })}
                      </span>
                      <Badge variant={statusVariant(s.status)}>{statusLabel(s.status)}</Badge>
                    </div>
                    <div className="mt-1 text-sm text-muted-foreground">
                      Com {s.workerName} · {s.services.map((x) => x.name).join(", ")}
                    </div>
                  </div>
                  <div className="flex items-center gap-3">
                    {s.bruteValue !== s.netValue ? (
                      <div className="flex flex-col items-end leading-tight">
                        <span className="text-sm text-muted-foreground line-through">
                          {brl(s.bruteValue)}
                        </span>
                        <span className="font-display text-lg text-primary">{brl(s.netValue)}</span>
                      </div>
                    ) : (
                      <div className="font-display text-lg text-primary">{brl(s.netValue)}</div>
                    )}
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => cancel.mutate(s.id)}
                      disabled={cancel.isPending}
                    >
                      Cancelar
                    </Button>
                  </div>
                </CardContent>
              </Card>
            ))}
          </ul>
        )}
      </section>

      {past.length > 0 && (
        <section>
          <h2 className="mb-3 font-display text-xl">Histórico</h2>
          <ul className="grid gap-3">
            {past.map((s) => (
              <Card key={s.id} className="opacity-80">
                <CardContent className="flex items-center justify-between p-5 text-sm">
                  <div>
                    <div className="font-medium">
                      {format(new Date(s.scheduledAt), "dd/MM/yyyy 'às' HH:mm")}
                    </div>
                    <div className="text-muted-foreground">Com {s.workerName}</div>
                  </div>
                  <Badge variant={statusVariant(s.status)}>{statusLabel(s.status)}</Badge>
                </CardContent>
              </Card>
            ))}
          </ul>
        </section>
      )}
    </div>
  );
}
