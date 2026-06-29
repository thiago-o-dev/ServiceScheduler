import { createFileRoute, Link } from "@tanstack/react-router";
import { useSuspenseQuery } from "@tanstack/react-query";
import { CalendarHeart, Sparkles, Clock, MapPin, Phone } from "lucide-react";
import { AppHeader } from "@/components/shell/AppHeader";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { servicesApi } from "@/lib/api/services.api";
import { workersApi } from "@/lib/api/workers.api";
import { brl } from "@/lib/format";

const servicesQuery = {
  queryKey: ["services", "public"],
  queryFn: () => servicesApi.list(),
};
const workersQuery = {
  queryKey: ["workers", "public"],
  queryFn: () => workersApi.list(),
};

export const Route = createFileRoute("/")({
  head: () => ({
    meta: [
      { title: "Cabeleleira Leila — Salão & estética em São Paulo" },
      {
        name: "description",
        content:
          "Cortes, coloração, tratamentos e beleza com profissionais experientes. Agende online em poucos cliques.",
      },
    ],
  }),
  loader: ({ context }) => {
    context.queryClient.ensureQueryData(servicesQuery);
    context.queryClient.ensureQueryData(workersQuery);
  },
  component: Landing,
});

function Landing() {
  const { data: services } = useSuspenseQuery(servicesQuery);
  const { data: workers } = useSuspenseQuery(workersQuery);

  return (
    <div className="min-h-screen bg-background">
      <AppHeader />

      <section className="relative overflow-hidden border-b">
        <div className="absolute inset-0 -z-10 bg-[radial-gradient(ellipse_at_top_right,_var(--color-rose)_0%,_transparent_60%),radial-gradient(ellipse_at_bottom_left,_var(--color-gold)_0%,_transparent_55%)] opacity-30" />
        <div className="mx-auto grid max-w-7xl gap-12 px-6 py-20 md:grid-cols-2 md:items-center md:py-28">
          <div>
            <span className="inline-flex items-center gap-2 rounded-full border bg-card/60 px-3 py-1 text-xs uppercase tracking-[0.18em] text-muted-foreground">
              <Sparkles className="h-3 w-3" /> Bem-vinda ao nosso espaço
            </span>
            <h1 className="mt-5 font-display text-5xl leading-tight text-foreground md:text-6xl">
              Sua beleza, no tempo certo, com quem entende.
            </h1>
            <p className="mt-5 max-w-lg text-base text-muted-foreground">
              Cortes, coloração e tratamentos com profissionais especializadas.
              Reserve seu horário em segundos, escolha sua profissional favorita e cuide de você.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <Button size="lg" asChild>
                <Link to="/client/book">
                  <CalendarHeart className="h-4 w-4" /> Agendar horário
                </Link>
              </Button>
              <Button size="lg" variant="outline" asChild>
                <Link to="/auth/register">Criar conta</Link>
              </Button>
            </div>
            <div className="mt-10 grid gap-4 text-sm text-muted-foreground sm:grid-cols-3">
              <div className="flex items-center gap-2"><Clock className="h-4 w-4" /> Ter–Sáb · 9h às 18h</div>
              <div className="flex items-center gap-2"><MapPin className="h-4 w-4" /> Rua das Flores, 123</div>
              <div className="flex items-center gap-2"><Phone className="h-4 w-4" /> (11) 4002-8922</div>
            </div>
          </div>
          <div className="relative">
            <div className="grid grid-cols-2 gap-4">
              {workers.slice(0, 4).map((w, i) => (
                <Card key={w.id} className={i % 2 ? "translate-y-6" : ""}>
                  <CardContent className="p-5">
                    <div className="grid h-16 w-16 place-items-center rounded-full bg-secondary font-display text-2xl text-primary">
                      {w.name.charAt(0)}
                    </div>
                    <div className="mt-3 font-display text-lg text-foreground">{w.name}</div>
                    <div className="text-xs text-muted-foreground">{w.title ?? "Profissional"}</div>
                  </CardContent>
                </Card>
              ))}
            </div>
          </div>
        </div>
      </section>

      <section className="mx-auto max-w-7xl px-6 py-20">
        <div className="flex items-end justify-between gap-4 border-b pb-4">
          <div>
            <h2 className="font-display text-3xl text-foreground">Nossos serviços</h2>
            <p className="mt-1 text-sm text-muted-foreground">Preços a partir de — clique em agendar para ver horários.</p>
          </div>
          <Button variant="outline" asChild>
            <Link to="/client/book">Agendar</Link>
          </Button>
        </div>
        <div className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {services.map((s) => (
            <Card key={s.id} className="transition hover:shadow-md">
              <CardContent className="p-6">
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <div className="font-display text-xl text-foreground">{s.name}</div>
                    <p className="mt-1 text-sm text-muted-foreground">{s.description}</p>
                  </div>
                  <span className="rounded-full bg-secondary px-3 py-1 text-sm font-medium text-primary">
                    {brl(s.value)}
                  </span>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      </section>

      <footer className="border-t bg-card/40">
        <div className="mx-auto flex max-w-7xl flex-col items-center justify-between gap-4 px-6 py-10 text-sm text-muted-foreground sm:flex-row">
          <span>© {new Date().getFullYear()} Cabeleleira Leila · Todos os direitos reservados.</span>
          <span>Feito com carinho em São Paulo.</span>
        </div>
      </footer>
    </div>
  );
}
