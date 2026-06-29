import { createFileRoute } from "@tanstack/react-router";
import { useMutation, useQueryClient, useSuspenseQuery } from "@tanstack/react-query";
import { format } from "date-fns";
import { toast } from "sonner";
import { PageHeader } from "@/components/shell/PageHeader";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/components/ui/table";
import {
  DropdownMenu, DropdownMenuTrigger, DropdownMenuContent, DropdownMenuItem,
} from "@/components/ui/dropdown-menu";
import { schedulesApi } from "@/lib/api/schedules.api";
import { adminApi } from "@/lib/api/admin.api";
import { brl } from "@/lib/format";
import { statusLabel, statusVariant } from "@/components/schedule/statusBadge";
import type { ServiceLineStatus } from "@/lib/api/types";

const allSchedulesQ = { queryKey: ["schedules", "admin"], queryFn: () => schedulesApi.list() };

export const Route = createFileRoute("/admin/schedules")({
  head: () => ({ meta: [{ title: "Agendamentos — Admin" }] }),
  loader: ({ context }) => context.queryClient.ensureQueryData(allSchedulesQ),
  component: AdminSchedules,
});

const lineStatuses: ServiceLineStatus[] = ["Pending", "InProgress", "Done", "Cancelled"];

function AdminSchedules() {
  const { data } = useSuspenseQuery(allSchedulesQ);
  const queryClient = useQueryClient();

  const setStatus = useMutation({
    mutationFn: (v: { scheduleId: string; serviceId: string; status: ServiceLineStatus }) =>
      adminApi.setServiceStatus(v.scheduleId, v.serviceId, { status: v.status }),
    onSuccess: () => {
      toast.success("Status atualizado.");
      queryClient.invalidateQueries({ queryKey: ["schedules"] });
    },
  });
  const cancel = useMutation({
    mutationFn: (id: string) => schedulesApi.cancel(id),
    onSuccess: () => {
      toast.success("Agendamento cancelado.");
      queryClient.invalidateQueries({ queryKey: ["schedules"] });
    },
  });
  const confirm = useMutation({
    mutationFn: (id: string) => schedulesApi.confirm(id),
    onSuccess: () => {
      toast.success("Confirmado.");
      queryClient.invalidateQueries({ queryKey: ["schedules"] });
    },
  });

  return (
    <div className="space-y-8">
      <PageHeader title="Agendamentos" description="Todos os agendamentos do salão." />
      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Quando</TableHead>
                <TableHead>Cliente</TableHead>
                <TableHead>Profissional</TableHead>
                <TableHead>Serviços</TableHead>
                <TableHead className="text-right">Total</TableHead>
                <TableHead>Status</TableHead>
                <TableHead className="text-right">Ações</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.map((s) => (
                <TableRow key={s.id}>
                  <TableCell className="font-medium">
                    {format(new Date(s.scheduledAt), "dd/MM 'às' HH:mm")}
                  </TableCell>
                  <TableCell>{s.customerName}</TableCell>
                  <TableCell>{s.workerName}</TableCell>
                  <TableCell>
                    <div className="flex flex-wrap gap-1">
                      {s.services.map((line) => (
                        <DropdownMenu key={line.serviceId}>
                          <DropdownMenuTrigger asChild>
                            <button className="rounded-full bg-secondary px-2 py-0.5 text-xs hover:bg-primary/10">
                              {line.serviceName} · {line.status}
                            </button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent>
                            {lineStatuses.map((st) => (
                              <DropdownMenuItem
                                key={st}
                                onClick={() =>
                                  setStatus.mutate({ scheduleId: s.id, serviceId: line.serviceId, status: st })
                                }
                              >
                                {st}
                              </DropdownMenuItem>
                            ))}
                          </DropdownMenuContent>
                        </DropdownMenu>
                      ))}
                    </div>
                  </TableCell>
                  <TableCell className="text-right font-medium text-primary">{brl(s.netValue)}</TableCell>
                  <TableCell>
                    <Badge variant={statusVariant(s.status)}>{statusLabel(s.status)}</Badge>
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-1">
                      {s.status === "Pending" && (
                        <Button size="sm" variant="outline" onClick={() => confirm.mutate(s.id)}>
                          Confirmar
                        </Button>
                      )}
                      {s.status !== "Cancelled" && (
                        <Button size="sm" variant="ghost" onClick={() => cancel.mutate(s.id)}>
                          Cancelar
                        </Button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
              {data.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7} className="py-10 text-center text-sm text-muted-foreground">
                    Nenhum agendamento ainda.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
}
