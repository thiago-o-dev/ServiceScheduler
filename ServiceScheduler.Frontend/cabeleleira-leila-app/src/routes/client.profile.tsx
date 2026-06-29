import { createFileRoute } from "@tanstack/react-router";
import { useMutation, useQueryClient, useSuspenseQuery } from "@tanstack/react-query";
import { useState, useEffect } from "react";
import { toast } from "sonner";
import { PageHeader } from "@/components/shell/PageHeader";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { customersApi } from "@/lib/api/customers.api";
import { useAuth } from "@/lib/auth/auth-context";

export const Route = createFileRoute("/client/profile")({
  head: () => ({ meta: [{ title: "Meu perfil — Cabeleleira Leila" }] }),
  component: ProfilePage,
});

function ProfilePage() {
  const { user } = useAuth();
  const customerId = user?.customerId!;
  const queryClient = useQueryClient();
  const q = useSuspenseQuery({
    queryKey: ["customer", customerId],
    queryFn: () => customersApi.get(customerId),
  });
  const [name, setName] = useState(q.data?.name ?? "");
  const [phone, setPhone] = useState(q.data?.phone ?? "");
  useEffect(() => {
    setName(q.data?.name ?? "");
    setPhone(q.data?.phone ?? "");
  }, [q.data?.id, q.data?.name, q.data?.phone]);

  const update = useMutation({
    mutationFn: () => customersApi.update(customerId, { name, phone }),
    onSuccess: () => {
      toast.success("Perfil atualizado.");
      queryClient.invalidateQueries({ queryKey: ["customer", customerId] });
    },
  });

  return (
    <div className="space-y-8">
      <PageHeader title="Meu perfil" description="Mantenha seus dados sempre atualizados." />
      <Card className="max-w-xl">
        <CardContent className="space-y-4 p-6">
          <div className="space-y-2">
            <Label>Nome</Label>
            <Input value={name} onChange={(e) => setName(e.target.value)} />
          </div>
          <div className="space-y-2">
            <Label>Telefone</Label>
            <Input value={phone} onChange={(e) => setPhone(e.target.value)} />
          </div>
          <div className="space-y-2">
            <Label>E-mail</Label>
            <Input value={q.data?.email ?? ""} disabled />
          </div>
          <div className="space-y-2">
            <Label>CPF</Label>
            <Input value={q.data?.cpf ?? ""} disabled />
          </div>
          <Button onClick={() => update.mutate()} disabled={update.isPending}>
            {update.isPending ? "Salvando..." : "Salvar alterações"}
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}
