import { Link, useRouter } from "@tanstack/react-router";
import { Scissors, LogOut, User } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/lib/auth/auth-context";
import { cn } from "@/lib/utils";

interface NavItem {
  to: string;
  label: string;
}

const navByRole: Record<string, NavItem[]> = {
  Client: [
    { to: "/client/book", label: "Agendar" },
    { to: "/client/appointments", label: "Meus horários" },
    { to: "/client/profile", label: "Meu perfil" },
  ],
  Worker: [
    { to: "/worker/dashboard", label: "Dashboard" },
    { to: "/worker/agenda", label: "Agenda" },
    { to: "/worker/availability", label: "Disponibilidade" },
    { to: "/worker/services", label: "Serviços" },
  ],
  Admin: [
    { to: "/admin/dashboard", label: "Dashboard" },
    { to: "/admin/schedules", label: "Agendamentos" },
    { to: "/admin/workers", label: "Profissionais" },
    { to: "/admin/services", label: "Serviços" },
    { to: "/admin/customers", label: "Clientes" },
  ],
};

export function AppHeader() {
  const { user, role, logout } = useAuth();
  const router = useRouter();
  const items = role ? navByRole[role] ?? [] : [];

  return (
    <header className="sticky top-0 z-40 border-b bg-background/85 backdrop-blur">
      <div className="mx-auto flex h-16 max-w-7xl items-center justify-between gap-6 px-6">
        <Link to="/" className="flex items-center gap-2">
          <span className="grid h-9 w-9 place-items-center rounded-full bg-primary text-primary-foreground">
            <Scissors className="h-4 w-4" />
          </span>
          <div className="leading-tight">
            <div className="font-display text-lg">Cabeleleira Leila</div>
            <div className="text-[10px] uppercase tracking-[0.18em] text-muted-foreground">
              Salão & estética
            </div>
          </div>
        </Link>

        {user && (
          <nav className="hidden gap-1 md:flex">
            {items.map((it) => (
              <Link
                key={it.to}
                to={it.to}
                className={cn(
                  "rounded-md px-3 py-2 text-sm font-medium text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground",
                )}
                activeProps={{ className: "bg-secondary text-foreground" }}
              >
                {it.label}
              </Link>
            ))}
          </nav>
        )}

        <div className="flex items-center gap-2">
          {user ? (
            <>
              <div className="hidden text-right text-xs leading-tight sm:block">
                <div className="font-medium text-foreground">{user.name}</div>
                <div className="text-muted-foreground">{role}</div>
              </div>
              <Button
                variant="ghost"
                size="icon"
                onClick={() => {
                  logout();
                  router.navigate({ to: "/" });
                }}
                aria-label="Sair"
              >
                <LogOut className="h-4 w-4" />
              </Button>
            </>
          ) : (
            <>
              <Button variant="ghost" asChild>
                <Link to="/auth/login">
                  <User className="h-4 w-4" /> Entrar
                </Link>
              </Button>
              <Button asChild>
                <Link to="/auth/register">Criar conta</Link>
              </Button>
            </>
          )}
        </div>
      </div>
    </header>
  );
}
