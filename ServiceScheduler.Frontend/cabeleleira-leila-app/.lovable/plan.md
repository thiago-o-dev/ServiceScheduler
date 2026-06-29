
# Cabeleleira Leila — Salon Scheduling App

A bilingual-ready (pt-BR copy) salon app on the existing TanStack Start + React 19 + Vite stack. Three role experiences: Client, Worker (full), and Admin (dashboard + scaffolded admin actions). Every endpoint in `gateway-api.json` and `core-api.json` is wired through a typed API layer; the UI surfaces them via polished, task-focused flows.

## Visual direction

- Warm, feminine, salon-inspired palette: soft rose / champagne / deep plum on cream, gold accents. Avoid generic purple/blue.
- Typography: "Fraunces" (display) + "Inter" (body) loaded via `<link>` in `__root.tsx`.
- All colors as oklch tokens in `src/styles.css` (`--background`, `--primary`, `--accent`, plus `--gold`, `--rose`, `--plum`). No hardcoded hex in components.
- Slick calendar UX: month grid → day strip → available time slots, worker avatars, service chips. Built on shadcn `Calendar` + custom slot grid.

## Role model

`registerType` in `RegisterRequest`: `0 = Client`, `1 = Worker`, `2 = Admin (not yet supported by API for self-registration)`. Admins are seeded server-side; they log in via the same `/api/Auth/token` endpoint. Role is derived from JWT claims, stored in `localStorage` + React context.

## Routes

```
/                       → marketing landing
/auth/login             → /api/Auth/token
/auth/register          → register as Client (registerType=0)
/_client/book           → booking wizard (services → worker → date → time → confirm)
/_client/appointments   → my schedules, cancel
/_client/profile        → update customer
/_worker/dashboard      → weekly performance KPIs + chart (own slice of week)
/_worker/agenda         → week/day calendar of own schedules, confirm/cancel
/_worker/availability   → weekly available + one-off unavailable periods
/_worker/services       → read-only services + bundles
/_admin/dashboard       → weekly performance KPIs + chart (salon-wide)
/_admin/schedules       → list/edit any schedule, per-service status updates
/_admin/workers         → list/create/edit workers
/_admin/services        → list/create/edit services + bundles
/_admin/customers       → list/edit/delete customers
```

`_client`, `_worker`, `_admin` are pathless layout routes guarding by role.

## Weekly dashboard (worker + admin)

Both dashboards consume `GET /api/Dashboard/weekly-performance`. Same component, different framing:

- **Shared `WeeklyPerformance` component** (`src/components/dashboard/WeeklyPerformance.tsx`):
  - Week selector (prev / this week / next), defaults to current ISO week.
  - KPI tiles: total schedules, confirmed, cancelled, completed, gross revenue, net revenue, average ticket.
  - Bar chart by day of week (shadcn `Chart` + Recharts) showing schedules + revenue.
  - Top services & top workers lists (admin sees both; worker sees only own ranking among peers, anonymized if endpoint scopes server-side).
  - Empty / loading / error states via `Skeleton` and `EmptyState`.
- **Worker view** (`/_worker/dashboard`): same component, header "Sua semana", filters bound to the logged-in worker via JWT claim (no UI selector).
- **Admin view** (`/_admin/dashboard`): header "Visão geral da semana", optional worker filter dropdown (passed as query param to the endpoint if supported; otherwise client-side filter).
- Worker `/_worker/agenda` keeps a compact KPI strip pulling the same query (cache shared via TanStack Query key `["dashboard","weekly", weekStart, workerId?]`).

## Architecture

```
src/
  lib/
    api/
      client.ts                # fetch wrapper: base URL, auth header, error mapping
      auth.api.ts              # /api/Auth/token, /register
      customers.api.ts
      services.api.ts          # incl. /{id}/available-hours
      serviceBundles.api.ts
      workers.api.ts           # incl. available/unavailable periods, preempt
      schedules.api.ts         # incl. confirm, cancel
      admin.api.ts             # /api/admin/schedules + per-service status
      dashboard.api.ts         # /api/Dashboard/weekly-performance
      types.ts                 # TS types for every schema
    auth/
      auth-context.tsx, jwt.ts, use-require-role.ts
    time/
      duration.ts              # ISO ↔ .NET TimeSpan "HH:MM:SS"
      slots.ts, week.ts        # ISO week math for dashboard selector
  components/
    booking/                   # ServicePicker, WorkerPicker, DateStrip, SlotGrid, BookingSummary
    agenda/                    # WeekCalendar, AppointmentCard
    availability/              # WeeklyAvailabilityEditor, UnavailableRangeList
    dashboard/                 # WeeklyPerformance, KpiTile, WeekPicker, RevenueByDayChart
    admin/                     # ScheduleAdminTable, WorkerForm, ServiceForm, CustomerTable
    shell/                     # AppHeader, RoleNav, EmptyState, PageHeader
  routes/                      # as listed above
  styles.css
```

Data fetching: route loader calls `queryClient.ensureQueryData(opts)`, components read with `useSuspenseQuery(opts)`. Mutations use `useMutation` + targeted `invalidateQueries` (dashboard key invalidated after schedule create/confirm/cancel).

## Endpoint coverage map

| Endpoint | Used by |
| --- | --- |
| POST `/api/Auth/token`, `/register` | `/auth/login`, `/auth/register` |
| GET/POST/PUT/DELETE `/api/Customers` (+`/{id}`) | `/_client/profile`, `/_admin/customers` |
| GET/POST/PUT `/api/Services` (+`/{id}`) | landing, booking, `/_admin/services` |
| GET `/api/Services/{id}/available-hours` | booking SlotGrid |
| GET/POST/PUT `/api/ServiceBundles` (+`/{id}`) | booking bundles tab, `/_admin/services` |
| GET/POST/PUT `/api/Workers` (+`/{id}`) | booking WorkerPicker, `/_admin/workers` |
| GET/POST/DELETE `/api/Workers/{id}/available-periods` | `/_worker/availability` |
| POST/DELETE `/api/Workers/{id}/unavailable-periods`, POST `/preempt` | `/_worker/availability` (vacation, preempts conflicting schedules) |
| GET/POST/PUT `/api/Schedules` (+`/{id}`) | booking, `/_client/appointments`, `/_worker/agenda`, `/_admin/schedules` |
| PUT `/api/Schedules/{id}/confirm`, `/cancel` | worker agenda + client appointments + admin |
| PUT `/api/admin/schedules/{id}` | `/_admin/schedules` edit dialog (with `overrideNetValue`) |
| PATCH `/api/admin/schedules/{id}/services/{serviceId}/status` | `/_admin/schedules` per-line status menu |
| GET `/api/Dashboard/weekly-performance` | `/_worker/dashboard`, `/_admin/dashboard`, `/_worker/agenda` KPI strip |

## Configuration

- `VITE_GATEWAY_URL` and `VITE_CORE_API_URL` env vars (defaults: `http://localhost:7080` and `http://localhost:7080/servicescheduler-api`).
- `src/lib/api/client.ts` reads them and attaches `Authorization: Bearer <token>` from auth context.
- No Lovable Cloud / Supabase — all data lives in your external APIs.

## Out of scope (MVP)

- Admin self-registration (`registerType=2` not supported by API yet — admins seeded server-side).
- OAuth, password reset, email confirmation.
- Realtime updates (refetch on focus only).
- i18n switcher — copy is pt-BR.
