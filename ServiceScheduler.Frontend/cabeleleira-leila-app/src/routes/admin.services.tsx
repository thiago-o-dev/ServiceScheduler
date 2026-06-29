import { createFileRoute } from "@tanstack/react-router";
import { useMutation, useQueryClient, useSuspenseQuery } from "@tanstack/react-query";
import { useState } from "react";
import { Plus } from "lucide-react";
import { toast } from "sonner";
import { PageHeader } from "@/components/shell/PageHeader";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Badge } from "@/components/ui/badge";
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger,
} from "@/components/ui/dialog";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import { servicesApi } from "@/lib/api/services.api";
import { bundlesApi } from "@/lib/api/serviceBundles.api";
import { brl } from "@/lib/format";
import { minutesToTimeSpan, timeSpanToMinutes, formatDurationLabel } from "@/lib/time/duration";

const servicesQ = { queryKey: ["services"], queryFn: () => servicesApi.list() };
const bundlesQ = { queryKey: ["bundles"], queryFn: () => bundlesApi.list() };

export const Route = createFileRoute("/admin/services")({
  head: () => ({ meta: [{ title: "Serviços — Admin" }] }),
  loader: ({ context }) => {
    context.queryClient.ensureQueryData(servicesQ);
    context.queryClient.ensureQueryData(bundlesQ);
  },
  component: AdminServices,
});

function AdminServices() {
  return (
    <div className="space-y-8">
      <PageHeader title="Serviços & pacotes" description="Catálogo do salão." />
      <Tabs defaultValue="services">
        <TabsList>
          <TabsTrigger value="services">Serviços</TabsTrigger>
          <TabsTrigger value="bundles">Pacotes</TabsTrigger>
        </TabsList>
        <TabsContent value="services"><ServicesTab /></TabsContent>
        <TabsContent value="bundles"><BundlesTab /></TabsContent>
      </Tabs>
    </div>
  );
}

function ServicesTab() {
  const { data } = useSuspenseQuery(servicesQ);
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState({ name: "", description: "", value: 0, durationMin: 60 });
  const create = useMutation({
    mutationFn: () =>
      servicesApi.create({
        name: form.name,
        description: form.description,
        value: form.value,
        duration: minutesToTimeSpan(form.durationMin),
      }),
    onSuccess: () => {
      toast.success("Serviço criado.");
      setOpen(false);
      setForm({ name: "", description: "", value: 0, durationMin: 60 });
      queryClient.invalidateQueries({ queryKey: ["services"] });
    },
  });

  return (
    <div className="mt-4 space-y-4">
      <div className="flex justify-end">
        <Dialog open={open} onOpenChange={setOpen}>
          <DialogTrigger asChild>
            <Button><Plus className="h-4 w-4" /> Novo serviço</Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader><DialogTitle>Novo serviço</DialogTitle></DialogHeader>
            <div className="space-y-3">
              <div className="space-y-2"><Label>Nome</Label><Input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></div>
              <div className="space-y-2"><Label>Descrição</Label><Textarea value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} /></div>
              <div className="grid grid-cols-2 gap-2">
                <div className="space-y-2"><Label>Valor (R$)</Label><Input type="number" value={form.value} onChange={(e) => setForm({ ...form, value: Number(e.target.value) })} /></div>
                <div className="space-y-2"><Label>Duração (min)</Label><Input type="number" value={form.durationMin} onChange={(e) => setForm({ ...form, durationMin: Number(e.target.value) })} /></div>
              </div>
              <Button className="w-full" onClick={() => create.mutate()} disabled={create.isPending}>Salvar</Button>
            </div>
          </DialogContent>
        </Dialog>
      </div>
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {data.map((s) => (
          <Card key={s.id}>
            <CardContent className="p-5">
              <div className="flex items-start justify-between">
                <div className="font-display text-lg">{s.name}</div>
                <span className="font-medium text-primary">{brl(s.value)}</span>
              </div>
              <p className="mt-1 text-sm text-muted-foreground">{s.description}</p>
              <div className="mt-2 text-xs text-muted-foreground">
                {formatDurationLabel(timeSpanToMinutes(s.duration ?? "01:00:00"))}
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}

function BundlesTab() {
  const { data: bundles } = useSuspenseQuery(bundlesQ);
  const { data: services } = useSuspenseQuery(servicesQ);
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState({ name: "", description: "", discount: 10, serviceIds: [] as string[] });
  const create = useMutation({
    mutationFn: () => bundlesApi.create(form),
    onSuccess: () => {
      toast.success("Pacote criado.");
      setOpen(false);
      setForm({ name: "", description: "", discount: 10, serviceIds: [] });
      queryClient.invalidateQueries({ queryKey: ["bundles"] });
    },
  });
  const toggle = (id: string) =>
    setForm((f) => ({
      ...f,
      serviceIds: f.serviceIds.includes(id) ? f.serviceIds.filter((x) => x !== id) : [...f.serviceIds, id],
    }));

  return (
    <div className="mt-4 space-y-4">
      <div className="flex justify-end">
        <Dialog open={open} onOpenChange={setOpen}>
          <DialogTrigger asChild>
            <Button><Plus className="h-4 w-4" /> Novo pacote</Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader><DialogTitle>Novo pacote</DialogTitle></DialogHeader>
            <div className="space-y-3">
              <div className="space-y-2"><Label>Nome</Label><Input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></div>
              <div className="space-y-2"><Label>Descrição</Label><Textarea value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} /></div>
              <div className="space-y-2"><Label>Desconto (%)</Label><Input type="number" value={form.discount} onChange={(e) => setForm({ ...form, discount: Number(e.target.value) })} /></div>
              <div className="space-y-2">
                <Label>Serviços inclusos</Label>
                <div className="flex flex-wrap gap-2">
                  {services.map((s) => (
                    <button
                      key={s.id}
                      type="button"
                      onClick={() => toggle(s.id)}
                      className={`rounded-full border px-3 py-1 text-xs ${form.serviceIds.includes(s.id) ? "border-primary bg-primary text-primary-foreground" : "bg-card"}`}
                    >
                      {s.name}
                    </button>
                  ))}
                </div>
              </div>
              <Button className="w-full" onClick={() => create.mutate()} disabled={create.isPending}>Salvar</Button>
            </div>
          </DialogContent>
        </Dialog>
      </div>
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
                  return s ? <Badge key={sid} variant="secondary">{s.name}</Badge> : null;
                })}
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
