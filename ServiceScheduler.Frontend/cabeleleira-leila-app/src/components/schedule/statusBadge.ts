import type { ScheduleStatus } from "@/lib/api/types";

export function statusLabel(s: ScheduleStatus): string {
  return { Pending: "Pendente", Confirmed: "Confirmado", Cancelled: "Cancelado", Completed: "Concluído" }[s];
}
export function statusVariant(s: ScheduleStatus): "default" | "secondary" | "destructive" | "outline" {
  switch (s) {
    case "Confirmed": return "default";
    case "Completed": return "secondary";
    case "Cancelled": return "destructive";
    default: return "outline";
  }
}
