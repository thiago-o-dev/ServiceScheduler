import { createFileRoute } from "@tanstack/react-router";
import { WeeklyPerformance } from "@/components/dashboard/WeeklyPerformance";
import { useAuth } from "@/lib/auth/auth-context";

export const Route = createFileRoute("/worker/dashboard")({
  head: () => ({ meta: [{ title: "Dashboard — Cabeleleira Leila" }] }),
  component: WorkerDashboard,
});

function WorkerDashboard() {
  const { user } = useAuth();
  return (
    <WeeklyPerformance
      scope="worker"
      title="Sua semana"
      description="Performance da semana atual com seus atendimentos."
      workerId={user?.workerId}
    />
  );
}
