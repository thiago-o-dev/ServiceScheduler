import { createFileRoute } from "@tanstack/react-router";
import { useSuspenseQuery } from "@tanstack/react-query";
import { PageHeader } from "@/components/shell/PageHeader";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { servicesApi } from "@/lib/api/services.api";
import { bundlesApi } from "@/lib/api/serviceBundles.api";
import { brl } from "@/lib/format";
import { formatDurationLabel, timeSpanToMinutes } from "@/lib/time/duration";

const servicesQ = { queryKey: ["services"], queryFn: () => servicesApi.list() };
const bundlesQ = { queryKey: ["bundles"], queryFn: () => bundlesApi.list() };

export const Route = createFileRoute("/worker/services")({
  head: () => ({ meta: [{ title: "Serviços — Cabeleleira Leila" }] }),
  loader: ({ context }) => {
    context.queryClient.ensureQueryData(servicesQ);
    context.queryClient.ensureQueryData(bundlesQ);
  },
  component: ServicesPage,
});

function ServicesPage() {
  const { data: services } = useSuspenseQuery(servicesQ);
  const { data: bundles } = useSuspenseQuery(bundlesQ);
  return (
    <div className="space-y-8">
      <PageHeader title="Catálogo" description="Serviços e pacotes oferecidos pelo salão." />
      <section className="space-y-3">
        <h2 className="font-display text-xl">Serviços</h2>
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {services.map((s) => (
            <Card key={s.id}>
              <CardContent className="p-5">
                <div className="flex items-start justify-between">
                  <div className="font-display text-lg">{s.name}</div>
                  <span className="font-medium text-primary">{brl(s.value)}</span>
                </div>
                <p className="mt-1 text-sm text-muted-foreground">{s.description}</p>
                <div className="mt-2 text-xs text-muted-foreground">
                  Duração: {formatDurationLabel(timeSpanToMinutes(s.duration ?? "01:00:00"))}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      </section>
      <section className="space-y-3">
        <h2 className="font-display text-xl">Pacotes</h2>
        <div className="grid gap-3 md:grid-cols-2">
          {bundles.map((b) => (
            <Card key={b.id}>
              <CardContent className="p-5">
                <div className="flex items-start justify-between">
                  <div className="font-display text-lg">{b.name}</div>
                  <Badge>-{b.discount}%</Badge>
                </div>
                <p className="mt-1 text-sm text-muted-foreground">{b.description}</p>
                <div className="mt-2 flex flex-wrap gap-1">
                  {b.serviceIds.map((sid) => {
                    const s = services.find((x) => x.id === sid);
                    return s ? (
                      <Badge key={sid} variant="secondary">{s.name}</Badge>
                    ) : null;
                  })}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      </section>
    </div>
  );
}
