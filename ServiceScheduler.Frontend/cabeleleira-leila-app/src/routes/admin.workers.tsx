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
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger,
} from "@/components/ui/dialog";
import { workersApi } from "@/lib/api/workers.api";

const workersQ = { queryKey: ["workers"], queryFn: () => workersApi.list() };

export const Route = createFileRoute("/admin/workers")({
  head: () => ({ meta: [{ title: "Profissionais — Admin" }] }),
  loader: ({ context }) => context.queryClient.ensureQueryData(workersQ),
  component: AdminWorkers,
});

function AdminWorkers() {
  const { data } = useSuspenseQuery(workersQ);
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState({ name: "", email: "", phone: "", cpf: "" });
  const create = useMutation({
    mutationFn: () => workersApi.create(form),
    onSuccess: () => {
      toast.success("Profissional cadastrada.");
      setForm({ name: "", email: "", phone: "", cpf: "" });
      setOpen(false);
      queryClient.invalidateQueries({ queryKey: ["workers"] });
    },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <div className="space-y-8">
      <PageHeader
        title="Profissionais"
        description="Equipe do salão."
        actions={
          <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger asChild>
              <Button><Plus className="h-4 w-4" /> Adicionar</Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader><DialogTitle>Nova profissional</DialogTitle></DialogHeader>
              <div className="space-y-3">
                <Field label="Nome" value={form.name} onChange={(v) => setForm({ ...form, name: v })} />
                <Field label="E-mail" value={form.email} onChange={(v) => setForm({ ...form, email: v })} />
                <Field label="Telefone" value={form.phone} onChange={(v) => setForm({ ...form, phone: v })} />
                <Field label="CPF" value={form.cpf} onChange={(v) => setForm({ ...form, cpf: v })} />
                <Button className="w-full" onClick={() => create.mutate()} disabled={create.isPending}>
                  {create.isPending ? "Salvando..." : "Salvar"}
                </Button>
              </div>
            </DialogContent>
          </Dialog>
        }
      />
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {data.map((w) => (
          <Card key={w.id}>
            <CardContent className="p-5">
              <div className="grid h-12 w-12 place-items-center rounded-full bg-secondary font-display text-lg text-primary">
                {w.name.charAt(0)}
              </div>
              <div className="mt-3 font-display text-lg">{w.name}</div>
              <div className="text-sm text-muted-foreground">{w.title ?? "Profissional"}</div>
              <div className="mt-2 text-xs text-muted-foreground">{w.email}</div>
              <div className="text-xs text-muted-foreground">{w.phone}</div>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}

function Field({ label, value, onChange }: { label: string; value: string; onChange: (v: string) => void }) {
  return (
    <div className="space-y-2">
      <Label>{label}</Label>
      <Input value={value} onChange={(e) => onChange(e.target.value)} />
    </div>
  );
}
