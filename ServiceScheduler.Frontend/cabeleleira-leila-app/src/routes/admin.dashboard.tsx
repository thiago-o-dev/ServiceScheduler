import { createFileRoute } from "@tanstack/react-router";
import { WeeklyPerformance } from "@/components/dashboard/WeeklyPerformance";

export const Route = createFileRoute("/admin/dashboard")({
  head: () => ({ meta: [{ title: "Dashboard — Admin" }] }),
  component: AdminDashboard,
});

function AdminDashboard() {
  return (
    <WeeklyPerformance
      scope="admin"
      title="Visão geral da semana"
      description="Performance do salão inteiro, com receita, atendimentos e profissionais em destaque."
    />
  );
}
