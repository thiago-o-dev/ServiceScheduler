import { createFileRoute, Link, useRouter } from "@tanstack/react-router";
import { useState } from "react";
import { toast } from "sonner";
import { AppHeader } from "@/components/shell/AppHeader";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useAuth } from "@/lib/auth/auth-context";

export const Route = createFileRoute("/auth/login")({
  head: () => ({
    meta: [
      { title: "Entrar — Cabeleleira Leila" },
      { name: "description", content: "Acesse sua conta no salão Cabeleleira Leila." },
    ],
  }),
  component: LoginPage,
});

function LoginPage() {
  const { login } = useAuth();
  const router = useRouter();
  const [email, setEmail] = useState("cliente@demo.com");
  const [password, setPassword] = useState("demo1234");
  const [loading, setLoading] = useState(false);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      const claims = await login({ email, password });
      toast.success("Bem-vinda de volta!");
      const dest =
        claims?.role === "Worker"
          ? "/worker/dashboard"
          : claims?.role === "Admin"
            ? "/admin/dashboard"
            : "/client/book";
      router.navigate({ to: dest });
    } catch (err) {
      toast.error((err as Error).message || "Não foi possível entrar.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-background">
      <AppHeader />
      <main className="mx-auto flex max-w-md flex-col gap-6 px-6 py-16">
        <div>
          <h1 className="font-display text-3xl">Entrar</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Acesse sua conta para gerenciar seus horários.
          </p>
        </div>
        <Card>
          <CardContent className="p-6">
            <form className="space-y-4" onSubmit={onSubmit}>
              <div className="space-y-2">
                <Label htmlFor="email">E-mail</Label>
                <Input id="email" type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="password">Senha</Label>
                <Input id="password" type="password" required value={password} onChange={(e) => setPassword(e.target.value)} />
              </div>
              <Button type="submit" className="w-full" disabled={loading}>
                {loading ? "Entrando..." : "Entrar"}
              </Button>
            </form>
            <div className="mt-4 text-center text-sm text-muted-foreground">
              Sem conta?{" "}
              <Link to="/auth/register" className="font-medium text-primary hover:underline">
                Cadastre-se
              </Link>
            </div>
          </CardContent>
        </Card>
        <div className="rounded-lg border bg-card/60 p-4 text-xs text-muted-foreground">
          <div className="mb-1 font-medium text-foreground">Contas de demonstração</div>
          <ul className="space-y-1">
            <li><span className="font-mono">cliente@demo.com</span> · cliente</li>
            <li><span className="font-mono">leila@cabeleleiraleila.com</span> · profissional</li>
            <li><span className="font-mono">bruna@cabeleleiraleila.com</span> · profissional</li>
            <li><span className="font-mono">camila@cabeleleiraleila.com</span> · profissional</li>
            <li><span className="font-mono">admin@cabeleleiraleila.com</span> · administradora</li>
            <li>Senha: <span className="font-mono">demo1234</span></li>
          </ul>
        </div>
      </main>
    </div>
  );
}
