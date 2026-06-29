import { useState } from "react";
import { useSuspenseQuery } from "@tanstack/react-query";
import { ChevronLeft, ChevronRight, CalendarDays, TrendingUp, CheckCircle2, XCircle, Sparkles } from "lucide-react";
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { dashboardApi } from "@/lib/api/dashboard.api";
import { getWeekStart, formatWeekRange, weekDays } from "@/lib/time/week";
import { brl, shortDay } from "@/lib/format";
import type { UUID } from "@/lib/api/types";

interface Props {
  title: string;
  description?: string;
  workerId?: UUID;
  scope: "worker" | "admin";
}

export function WeeklyPerformance({ title, description, workerId, scope }: Props) {
  const [weekStart, setWeekStart] = useState<Date>(() => getWeekStart(new Date()));
  const weekIso = weekStart.toISOString();
  const { data } = useSuspenseQuery({
    queryKey: ["dashboard", "weekly", weekIso, workerId ?? null],
    queryFn: () => dashboardApi.weekly(weekIso, workerId),
  });

  const chartData = data.byDay.map((d, i) => {
    const day = weekDays(weekStart)[i];
    return { day: shortDay(day.getDay()), schedules: d.schedules, revenue: d.revenue };
  });

  const shift = (days: number) =>
    setWeekStart(new Date(weekStart.getTime() + days * 86_400_000));

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 border-b pb-6 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="font-display text-3xl text-foreground">{title}</h1>
          {description && <p className="mt-1 text-sm text-muted-foreground">{description}</p>}
        </div>
        <div className="flex items-center gap-2 rounded-full border bg-card px-2 py-1">
          <Button variant="ghost" size="icon" onClick={() => shift(-7)} aria-label="Semana anterior">
            <ChevronLeft className="h-4 w-4" />
          </Button>
          <button
            onClick={() => setWeekStart(getWeekStart(new Date()))}
            className="min-w-[180px] text-center text-sm font-medium hover:underline"
          >
            {formatWeekRange(weekStart)}
          </button>
          <Button variant="ghost" size="icon" onClick={() => shift(7)} aria-label="Próxima semana">
            <ChevronRight className="h-4 w-4" />
          </Button>
        </div>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <KpiTile icon={<CalendarDays className="h-4 w-4" />} label="Agendamentos" value={data.totals.schedules} sub={`${data.totals.confirmed} confirmados`} />
        <KpiTile icon={<CheckCircle2 className="h-4 w-4 text-emerald-600" />} label="Concluídos" value={data.totals.completed} />
        <KpiTile icon={<XCircle className="h-4 w-4 text-destructive" />} label="Cancelados" value={data.totals.cancelled} />
        <KpiTile icon={<TrendingUp className="h-4 w-4 text-primary" />} label="Receita líquida" value={brl(data.totals.netRevenue)} sub={`Ticket médio ${brl(data.totals.averageTicket)}`} />
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <Card className="lg:col-span-2">
          <CardContent className="p-6">
            <div className="mb-4 flex items-center justify-between">
              <div className="font-display text-lg">Receita por dia</div>
              <div className="text-xs text-muted-foreground">Líquido (R$)</div>
            </div>
            <div className="h-64 w-full">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={chartData} margin={{ left: -20, right: 12 }}>
                  <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="var(--color-border)" />
                  <XAxis dataKey="day" tickLine={false} axisLine={false} stroke="var(--color-muted-foreground)" />
                  <YAxis tickLine={false} axisLine={false} stroke="var(--color-muted-foreground)" />
                  <Tooltip
                    cursor={{ fill: "var(--color-muted)", opacity: 0.5 }}
                    contentStyle={{ borderRadius: 8, border: "1px solid var(--color-border)", background: "var(--color-popover)", color: "var(--color-popover-foreground)" }}
                    formatter={(v: number) => brl(v)}
                  />
                  <Bar dataKey="revenue" fill="var(--color-primary)" radius={[6, 6, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="mb-4 flex items-center gap-2 font-display text-lg">
              <Sparkles className="h-4 w-4 text-primary" /> Top serviços
            </div>
            {data.topServices.length === 0 ? (
              <p className="text-sm text-muted-foreground">Sem dados nesta semana.</p>
            ) : (
              <ul className="space-y-3">
                {data.topServices.map((s) => (
                  <li key={s.serviceId} className="flex items-center justify-between text-sm">
                    <span className="text-foreground">{s.serviceName}</span>
                    <span className="font-medium text-primary">{brl(s.revenue)}</span>
                  </li>
                ))}
              </ul>
            )}
          </CardContent>
        </Card>
      </div>

      {scope === "admin" && (
        <Card>
          <CardContent className="p-6">
            <div className="mb-4 font-display text-lg">Profissionais da semana</div>
            {data.topWorkers.length === 0 ? (
              <p className="text-sm text-muted-foreground">Sem dados nesta semana.</p>
            ) : (
              <ul className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                {data.topWorkers.map((w) => (
                  <li key={w.workerId} className="flex items-center justify-between rounded-lg border bg-card/40 p-3">
                    <div>
                      <div className="font-medium">{w.workerName}</div>
                      <div className="text-xs text-muted-foreground">{w.count} atendimentos</div>
                    </div>
                    <div className="font-display text-primary">{brl(w.revenue)}</div>
                  </li>
                ))}
              </ul>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  );
}

function KpiTile({
  icon,
  label,
  value,
  sub,
}: {
  icon: React.ReactNode;
  label: string;
  value: string | number;
  sub?: string;
}) {
  return (
    <Card>
      <CardContent className="p-5">
        <div className="flex items-center gap-2 text-xs uppercase tracking-wider text-muted-foreground">
          {icon} {label}
        </div>
        <div className="mt-2 font-display text-3xl text-foreground">{value}</div>
        {sub && <div className="mt-1 text-xs text-muted-foreground">{sub}</div>}
      </CardContent>
    </Card>
  );
}
