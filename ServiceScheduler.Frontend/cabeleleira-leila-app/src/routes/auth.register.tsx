import { createFileRoute, Link, useRouter } from "@tanstack/react-router";
import { useState } from "react";
import { toast } from "sonner";
import { AppHeader } from "@/components/shell/AppHeader";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { useAuth } from "@/lib/auth/auth-context";
import { RegisterType } from "@/lib/api/types";
import { Eye, EyeOff } from "lucide-react";

export const Route = createFileRoute("/auth/register")({
  head: () => ({
    meta: [
      { title: "Criar conta — Cabeleleira Leila" },
      {
        name: "description",
        content: "Crie sua conta para agendar serviços ou trabalhar conosco.",
      },
    ],
  }),
  component: RegisterPage,
});

function RegisterPage() {
  const { register } = useAuth();
  const router = useRouter();
  const [form, setForm] = useState({
    name: "",
    email: "",
    password: "",
    phone: "",
    cpf: "",
    registerType: String(RegisterType.Client),
  });
  const [loading, setLoading] = useState(false);

  const onSubmit = async (e: React.SubmitEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      const claims = await register({
        name: form.name,
        email: form.email,
        password: form.password,
        phone: form.phone,
        cpf: form.cpf,
        registerType: Number(form.registerType) as RegisterType,
      });
      toast.success("Conta criada com sucesso!");
      const dest = claims?.role === "Worker" ? "/worker/dashboard" : "/client/book";
      router.navigate({ to: dest });
    } catch (err) {
      toast.error((err as Error).message || "Não foi possível cadastrar.");
    } finally {
      setLoading(false);
    }
  };

  const onlyDigits = (value: string) => value.replace(/\D/g, "");

  const formatCpf = (value: string) => {
    const digits = onlyDigits(value).slice(0, 11);

    return digits
      .replace(/^(\d{3})(\d)/, "$1.$2")
      .replace(/^(\d{3})\.(\d{3})(\d)/, "$1.$2.$3")
      .replace(/\.(\d{3})(\d)/, ".$1-$2");
  };

  const formatPhone = (value: string) => {
    // Store with country code (55)
    let digits = onlyDigits(value);

    if (!digits.startsWith("55")) {
      digits = "55" + digits;
    }

    digits = digits.slice(0, 13); // 55 + AA + 9 + 8 digits

    const local = digits.slice(2);

    if (!local.length) return "+55";

    if (local.length <= 2) {
      return `+55 (${local}`;
    }

    if (local.length <= 7) {
      return `+55 (${local.slice(0, 2)}) ${local.slice(2)}`;
    }

    return `+55 (${local.slice(0, 2)}) ${local.slice(2, 7)} ${local.slice(7)}`;
  };

  const isWorker = Number(form.registerType) === RegisterType.Worker;
  const [showPassword, setShowPassword] = useState(false);

  return (
    <div className="min-h-screen bg-background">
      <AppHeader />
      <main className="mx-auto flex max-w-md flex-col gap-6 px-6 py-16">
        <div>
          <h1 className="font-display text-3xl">Criar conta</h1>
          <p className="mt-1 text-sm text-muted-foreground">Comece agora seu cadastro.</p>
        </div>
        <Card>
          <CardContent className="p-6">
            <form className="space-y-4" onSubmit={onSubmit}>
              <div className="space-y-2">
                <Label>Tipo de conta</Label>
                <RadioGroup
                  value={form.registerType}
                  onValueChange={(v) =>
                    setForm((prev) => ({
                      ...prev,
                      registerType: v,
                      //cpf: Number(v) === RegisterType.Worker ? prev.cpf : "",
                    }))
                  }
                  className="grid grid-cols-2 gap-2"
                >
                  <Label className="flex cursor-pointer items-center gap-2 rounded-md border p-3 has-data-[state=checked]:border-primary has-data-[state=checked]:bg-secondary">
                    <RadioGroupItem value={String(RegisterType.Client)} /> Cliente
                  </Label>
                  <Label className="flex cursor-pointer items-center gap-2 rounded-md border p-3 has-data-[state=checked]:border-primary has-data-[state=checked]:bg-secondary">
                    <RadioGroupItem value={String(RegisterType.Worker)} /> Profissional
                  </Label>
                </RadioGroup>
                <p className="text-xs text-muted-foreground">
                  Cadastro de administrador não é permitido — entre em contato com a gestão.
                </p>
              </div>
              <div className="space-y-2">
                <Label htmlFor="name">Nome completo</Label>
                <Input
                  id="name"
                  required
                  value={form.name}
                  onChange={(e) => setForm({ ...form, name: e.target.value })}
                />
              </div>
              <div className={`grid gap-3 ${isWorker ? "grid-cols-2" : "grid-cols-1"}`}>
                <div className="space-y-2">
                  <Label htmlFor="phone">Telefone</Label>
                  <Input
                    id="phone"
                    required
                    inputMode="numeric"
                    autoComplete="tel"
                    value={formatPhone(form.phone)}
                    onChange={(e) => {
                      let digits = onlyDigits(e.target.value);

                      if (!digits.startsWith("55")) {
                        digits = "55" + digits;
                      }

                      setForm({
                        ...form,
                        phone: digits,
                      });
                    }}
                  />
                </div>

                {isWorker && (
                  <div className="space-y-2">
                    <Label htmlFor="cpf">CPF</Label>
                    <Input
                      id="cpf"
                      required={isWorker}
                      inputMode="numeric"
                      autoComplete="off"
                      value={formatCpf(form.cpf)}
                      onChange={(e) =>
                        setForm({
                          ...form,
                          cpf: onlyDigits(e.target.value).slice(0, 11),
                        })
                      }
                    />
                  </div>
                )}
              </div>
              <div className="space-y-2">
                <Label htmlFor="email">E-mail</Label>
                <Input
                  id="email"
                  type="email"
                  required
                  value={form.email}
                  onChange={(e) => setForm({ ...form, email: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="password">Senha</Label>

                <div className="relative">
                  <Input
                    id="password"
                    type={showPassword ? "text" : "password"}
                    required
                    minLength={6}
                    value={form.password}
                    onChange={(e) => setForm({ ...form, password: e.target.value })}
                    className="pr-10"
                  />

                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="absolute right-0 top-0 h-full px-3 hover:bg-transparent"
                    onClick={() => setShowPassword((v) => !v)}
                  >
                    {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                    <span className="sr-only">
                      {showPassword ? "Ocultar senha" : "Mostrar senha"}
                    </span>
                  </Button>
                </div>
              </div>
              <Button type="submit" className="w-full" disabled={loading}>
                {loading ? "Criando..." : "Criar conta"}
              </Button>
            </form>
            <div className="mt-4 text-center text-sm text-muted-foreground">
              Já tem conta?{" "}
              <Link to="/auth/login" className="font-medium text-primary hover:underline">
                Entrar
              </Link>
            </div>
          </CardContent>
        </Card>
      </main>
    </div>
  );
}
