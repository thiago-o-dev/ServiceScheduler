import { createFileRoute, Outlet } from "@tanstack/react-router";
import { AppHeader } from "@/components/shell/AppHeader";
import { guardRole } from "@/lib/auth/guards";

export const Route = createFileRoute("/client")({
  beforeLoad: guardRole("Client"),
  component: ClientLayout,
});

function ClientLayout() {
  return (
    <div className="min-h-screen bg-background">
      <AppHeader />
      <main className="mx-auto max-w-7xl px-6 py-10">
        <Outlet />
      </main>
    </div>
  );
}
