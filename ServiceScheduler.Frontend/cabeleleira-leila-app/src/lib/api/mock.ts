// In-memory mock backend that mirrors the salon OpenAPI contract.
// Persists in localStorage so the preview is stable across refreshes.

import type {
  AddAvailablePeriodRequest,
  AddUnavailablePeriodRequest,
  AdminUpdateScheduleRequest,
  AvailableHoursResponse,
  AvailablePeriod,
  CreateCustomerCommand,
  CreateScheduleCommand,
  CreateServiceBundleCommand,
  CreateServiceCommand,
  CreateWorkerCommand,
  Customer,
  RegisterRequest,
  Schedule,
  Service,
  ServiceBundle,
  TokenRequest,
  TokenResponse,
  UnavailablePeriod,
  UpdateCustomerRequest,
  UpdateScheduleRequest,
  UpdateServiceBundleRequest,
  UpdateServiceRequest,
  UpdateServiceStatusRequest,
  UpdateWorkerRequest,
  UUID,
  WeeklyPerformance,
  Worker,
} from "./types";
import { DayOfWeek, RegisterType } from "./types";

// ---------- Storage ----------
const KEY = "leila.mock.db.v1";
const TOKENS_KEY = "leila.mock.tokens.v1";

type Role = "Client" | "Worker" | "Admin";
interface MockUser {
  id: UUID;
  name: string;
  email: string;
  password: string;
  role: Role;
  customerId?: UUID;
  workerId?: UUID;
}
interface DB {
  users: MockUser[];
  customers: Customer[];
  workers: Worker[];
  services: Service[];
  bundles: ServiceBundle[];
  availablePeriods: Record<UUID, AvailablePeriod[]>;
  unavailablePeriods: Record<UUID, UnavailablePeriod[]>;
  schedules: Schedule[];
}

const uid = () =>
  (globalThis.crypto?.randomUUID?.() ??
    "xxxxxxxxxxxx4xxxyxxxxxxxxxxxxxxx".replace(/[xy]/g, (c) => {
      const r = (Math.random() * 16) | 0;
      const v = c === "x" ? r : (r & 0x3) | 0x8;
      return v.toString(16);
    })) as UUID;

function seedDB(): DB {
  const workers: Worker[] = [
    { id: uid(), name: "Leila Martins", email: "leila@cabeleleiraleila.com", phone: "(11) 99999-0001", cpf: "111.111.111-11", title: "Cabeleireira sênior" },
    { id: uid(), name: "Bruna Costa", email: "bruna@cabeleleiraleila.com", phone: "(11) 99999-0002", cpf: "222.222.222-22", title: "Colorista" },
    { id: uid(), name: "Camila Souza", email: "camila@cabeleleiraleila.com", phone: "(11) 99999-0003", cpf: "333.333.333-33", title: "Manicure & estética" },
  ];
  const services: Service[] = [
    { id: uid(), name: "Corte feminino", description: "Corte com lavagem e finalização.", value: 90, duration: "00:45:00" },
    { id: uid(), name: "Escova", description: "Escova modeladora.", value: 70, duration: "00:40:00" },
    { id: uid(), name: "Coloração", description: "Coloração completa com produtos premium.", value: 220, duration: "02:00:00" },
    { id: uid(), name: "Hidratação profunda", description: "Tratamento reparador.", value: 110, duration: "00:50:00" },
    { id: uid(), name: "Manicure", description: "Manicure clássica.", value: 45, duration: "00:35:00" },
    { id: uid(), name: "Pedicure", description: "Pedicure spa.", value: 55, duration: "00:45:00" },
  ];
  const bundles: ServiceBundle[] = [
    { id: uid(), name: "Dia da noiva", description: "Pacote completo para o grande dia.", serviceIds: [services[0].id, services[1].id, services[3].id], discount: 15 },
    { id: uid(), name: "Mãos & pés", description: "Manicure + pedicure com desconto.", serviceIds: [services[4].id, services[5].id], discount: 10 },
  ];

  const availablePeriods: DB["availablePeriods"] = {};
  for (const w of workers) {
    availablePeriods[w.id] = [DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday].map((d) => ({
      id: uid(),
      dayOfWeek: d,
      startTime: "09:00",
      endTime: "18:00",
    }));
  }

  const customer: Customer = { id: uid(), name: "Cliente Demonstração", email: "cliente@demo.com", phone: "(11) 90000-0000", cpf: "000.000.000-00" };

  const users: MockUser[] = [
    { id: uid(), name: customer.name, email: customer.email, password: "demo1234", role: "Client", customerId: customer.id },
    { id: uid(), name: workers[0].name, email: workers[0].email, password: "demo1234", role: "Worker", workerId: workers[0].id },
    { id: uid(), name: "Administradora", email: "admin@cabeleleiraleila.com", password: "demo1234", role: "Admin" },
  ];

  return {
    users,
    customers: [customer],
    workers,
    services,
    bundles,
    availablePeriods,
    unavailablePeriods: Object.fromEntries(workers.map((w) => [w.id, []])),
    schedules: [],
  };
}

function loadDB(): DB {
  if (typeof window === "undefined") return seedDB();
  const raw = window.localStorage.getItem(KEY);
  if (!raw) {
    const fresh = seedDB();
    window.localStorage.setItem(KEY, JSON.stringify(fresh));
    return fresh;
  }
  try {
    return JSON.parse(raw) as DB;
  } catch {
    const fresh = seedDB();
    window.localStorage.setItem(KEY, JSON.stringify(fresh));
    return fresh;
  }
}
function saveDB(db: DB) {
  if (typeof window === "undefined") return;
  window.localStorage.setItem(KEY, JSON.stringify(db));
}

// ---------- Tokens ----------
interface MockToken {
  sub: UUID;
  role: Role;
  name: string;
  email: string;
  customerId?: UUID;
  workerId?: UUID;
  exp: number;
}
function loadTokens(): Record<string, MockToken> {
  if (typeof window === "undefined") return {};
  const raw = window.localStorage.getItem(TOKENS_KEY);
  return raw ? (JSON.parse(raw) as Record<string, MockToken>) : {};
}
function saveTokens(t: Record<string, MockToken>) {
  if (typeof window === "undefined") return;
  window.localStorage.setItem(TOKENS_KEY, JSON.stringify(t));
}
function b64url(s: string): string {
  return btoa(s).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}
function mintToken(user: MockUser): string {
  const claims: MockToken = {
    sub: user.id,
    role: user.role,
    name: user.name,
    email: user.email,
    customerId: user.customerId,
    workerId: user.workerId,
    exp: Math.floor(Date.now() / 1000) + 60 * 60 * 24 * 7,
  };
  const header = b64url(JSON.stringify({ alg: "none", typ: "JWT" }));
  const payload = b64url(JSON.stringify(claims));
  const token = `${header}.${payload}.mock`;
  const tokens = loadTokens();
  tokens[token] = claims;
  saveTokens(tokens);
  return token;
}
function readToken(token: string | null): MockToken | null {
  if (!token) return null;
  const parts = token.split(".");
  if (parts.length !== 3) return null;
  try {
    const json = atob(parts[1].replace(/-/g, "+").replace(/_/g, "/"));
    return JSON.parse(json) as MockToken;
  } catch {
    return null;
  }
}

// ---------- Pricing & scheduling ----------
function computeScheduleTotals(
  db: DB,
  serviceIds: UUID[],
  overrideNetValue?: number | null,
): { gross: number; net: number } {
  const gross = serviceIds.reduce((sum, id) => sum + (db.services.find((s) => s.id === id)?.value ?? 0), 0);
  if (overrideNetValue != null) return { gross, net: overrideNetValue };
  // discount from any matching bundle whose services are a subset of selection
  const bundle = db.bundles.find((b) =>
    b.serviceIds.length > 1 && b.serviceIds.every((sid) => serviceIds.includes(sid)),
  );
  const discount = bundle?.discount ?? 0;
  return { gross, net: +(gross * (1 - discount / 100)).toFixed(2) };
}

function buildSchedule(
  db: DB,
  cmd: CreateScheduleCommand | (AdminUpdateScheduleRequest & { id?: UUID }) | UpdateScheduleRequest,
  base: Partial<Schedule> & { id?: UUID; customerId?: UUID; workerId?: UUID } = {},
): Schedule {
  const customerId = (cmd as CreateScheduleCommand).customerId ?? base.customerId!;
  const workerId = (cmd as CreateScheduleCommand).workerId ?? base.workerId!;
  const customer = db.customers.find((c) => c.id === customerId);
  const worker = db.workers.find((w) => w.id === workerId);
  const services = (cmd as CreateScheduleCommand).serviceIds.map((sid) => {
    const s = db.services.find((x) => x.id === sid);
    return {
      serviceId: sid,
      serviceName: s?.name ?? "Serviço",
      value: s?.value ?? 0,
      status: "Pending" as const,
    };
  });
  const override = (cmd as AdminUpdateScheduleRequest).overrideNetValue ?? null;
  const totals = computeScheduleTotals(db, (cmd as CreateScheduleCommand).serviceIds, override);
  return {
    id: base.id ?? uid(),
    customerId,
    customerName: customer?.name ?? "Cliente",
    workerId,
    workerName: worker?.name ?? "Profissional",
    services,
    scheduledAt: (cmd as CreateScheduleCommand).scheduledAt,
    duration: (cmd as CreateScheduleCommand).duration,
    status: base.status ?? "Pending",
    grossValue: totals.gross,
    netValue: totals.net,
  };
}

function computeAvailableHours(db: DB, serviceId: UUID, dateISO: string, workerId?: UUID): AvailableHoursResponse {
  const service = db.services.find((s) => s.id === serviceId);
  if (!service) return { date: dateISO, slots: [] };
  const [h, m] = (service.duration ?? "01:00:00").split(":").map(Number);
  const durationMs = ((h || 0) * 60 + (m || 0)) * 60_000;
  const target = new Date(dateISO + "T00:00:00");
  const dow = target.getDay() as DayOfWeek;
  const workers = workerId ? db.workers.filter((w) => w.id === workerId) : db.workers;
  const slots: AvailableHoursResponse["slots"] = [];
  for (const w of workers) {
    const periods = (db.availablePeriods[w.id] ?? []).filter((p) => p.dayOfWeek === dow);
    const unavail = db.unavailablePeriods[w.id] ?? [];
    for (const p of periods) {
      const [sh, sm] = p.startTime.split(":").map(Number);
      const [eh, em] = p.endTime.split(":").map(Number);
      const start = new Date(target);
      start.setHours(sh, sm, 0, 0);
      const end = new Date(target);
      end.setHours(eh, em, 0, 0);
      for (let t = start.getTime(); t + durationMs <= end.getTime(); t += 30 * 60_000) {
        const slotStart = new Date(t);
        const slotEnd = new Date(t + durationMs);
        // skip unavailable
        const blocked = unavail.some((u) => {
          const us = new Date(u.start).getTime();
          const ue = new Date(u.end).getTime();
          return slotStart.getTime() < ue && slotEnd.getTime() > us;
        });
        if (blocked) continue;
        // skip overlapping schedules
        const conflict = db.schedules.some((s) => {
          if (s.workerId !== w.id || s.status === "Cancelled") return false;
          const [dh, dm] = s.duration.split(":").map(Number);
          const ss = new Date(s.scheduledAt).getTime();
          const se = ss + ((dh || 0) * 60 + (dm || 0)) * 60_000;
          return slotStart.getTime() < se && slotEnd.getTime() > ss;
        });
        if (conflict) continue;
        // skip past
        if (slotStart.getTime() < Date.now()) continue;
        slots.push({ start: slotStart.toISOString(), end: slotEnd.toISOString(), workerId: w.id });
      }
    }
  }
  return { date: dateISO, slots };
}

function computeWeeklyPerformance(db: DB, weekStart: string, workerId?: UUID): WeeklyPerformance {
  const start = new Date(weekStart);
  const end = new Date(start.getTime() + 7 * 86_400_000);
  const inWeek = db.schedules.filter((s) => {
    if (workerId && s.workerId !== workerId) return false;
    const t = new Date(s.scheduledAt).getTime();
    return t >= start.getTime() && t < end.getTime();
  });
  const totals = {
    schedules: inWeek.length,
    confirmed: inWeek.filter((s) => s.status === "Confirmed").length,
    cancelled: inWeek.filter((s) => s.status === "Cancelled").length,
    completed: inWeek.filter((s) => s.status === "Completed").length,
    grossRevenue: +inWeek.filter((s) => s.status !== "Cancelled").reduce((sum, s) => sum + s.grossValue, 0).toFixed(2),
    netRevenue: +inWeek.filter((s) => s.status !== "Cancelled").reduce((sum, s) => sum + s.netValue, 0).toFixed(2),
    averageTicket: 0,
  };
  const billable = inWeek.filter((s) => s.status !== "Cancelled");
  totals.averageTicket = billable.length ? +(totals.netRevenue / billable.length).toFixed(2) : 0;

  const byDay = Array.from({ length: 7 }, (_, i) => {
    const day = new Date(start.getTime() + i * 86_400_000);
    const key = day.toISOString().slice(0, 10);
    const dayItems = inWeek.filter((s) => s.scheduledAt.slice(0, 10) === key && s.status !== "Cancelled");
    return {
      date: key,
      schedules: dayItems.length,
      revenue: +dayItems.reduce((sum, s) => sum + s.netValue, 0).toFixed(2),
    };
  });

  const svcMap = new Map<UUID, { serviceName: string; count: number; revenue: number }>();
  for (const s of inWeek.filter((x) => x.status !== "Cancelled")) {
    for (const line of s.services) {
      const cur = svcMap.get(line.serviceId) ?? { serviceName: line.serviceName, count: 0, revenue: 0 };
      cur.count += 1;
      cur.revenue += line.value;
      svcMap.set(line.serviceId, cur);
    }
  }
  const topServices = [...svcMap.entries()]
    .map(([serviceId, v]) => ({ serviceId, ...v, revenue: +v.revenue.toFixed(2) }))
    .sort((a, b) => b.revenue - a.revenue)
    .slice(0, 5);

  const workerMap = new Map<UUID, { workerName: string; count: number; revenue: number }>();
  for (const s of inWeek.filter((x) => x.status !== "Cancelled")) {
    const cur = workerMap.get(s.workerId) ?? { workerName: s.workerName, count: 0, revenue: 0 };
    cur.count += 1;
    cur.revenue += s.netValue;
    workerMap.set(s.workerId, cur);
  }
  const topWorkers = [...workerMap.entries()]
    .map(([workerId, v]) => ({ workerId, ...v, revenue: +v.revenue.toFixed(2) }))
    .sort((a, b) => b.revenue - a.revenue);

  return {
    weekStart: start.toISOString(),
    weekEnd: end.toISOString(),
    totals,
    byDay,
    topServices,
    topWorkers,
  };
}

// ---------- Router ----------
interface HandleArgs {
  path: string;
  method: string;
  body?: unknown;
  query?: Record<string, string | number | undefined | null>;
  target: "gateway" | "core";
  token: string | null;
}

async function delay<T>(value: T): Promise<T> {
  await new Promise((r) => setTimeout(r, 80));
  return value;
}

export async function mockHandle<T>(args: HandleArgs): Promise<T> {
  const db = loadDB();
  const claims = readToken(args.token);
  const { method, body, query } = args;
  const path = args.path.replace(/^\/+|\/+$/g, "");

  // ---- Gateway: Auth ----
  if (path === "api/Auth/token" && method === "POST") {
    const req = body as TokenRequest;
    const user = db.users.find((u) => u.email.toLowerCase() === req.email.toLowerCase() && u.password === req.password);
    if (!user) throw new Error("Credenciais inválidas");
    const token = mintToken(user);
    return delay({ token } as TokenResponse) as Promise<T>;
  }
  if (path === "api/Auth/register" && method === "POST") {
    const req = body as RegisterRequest;
    if (db.users.find((u) => u.email.toLowerCase() === req.email.toLowerCase())) {
      throw new Error("E-mail já cadastrado");
    }
    if (req.registerType === RegisterType.Admin) {
      throw new Error("Cadastro de administrador não suportado");
    }
    const newUser: MockUser = {
      id: uid(),
      name: req.name,
      email: req.email,
      password: req.password,
      role: req.registerType === RegisterType.Worker ? "Worker" : "Client",
    };
    if (newUser.role === "Client") {
      const customer: Customer = { id: uid(), name: req.name, email: req.email, phone: req.phone ?? "", cpf: req.cpf ?? "" };
      db.customers.push(customer);
      newUser.customerId = customer.id;
    } else {
      const worker: Worker = { id: uid(), name: req.name, email: req.email, phone: req.phone ?? "", cpf: req.cpf ?? "" };
      db.workers.push(worker);
      db.availablePeriods[worker.id] = [];
      db.unavailablePeriods[worker.id] = [];
      newUser.workerId = worker.id;
    }
    db.users.push(newUser);
    saveDB(db);
    const token = mintToken(newUser);
    return delay({ token } as TokenResponse) as Promise<T>;
  }

  // ---- Customers ----
  if (path === "api/Customers" && method === "GET") return delay(db.customers as unknown) as Promise<T>;
  if (path === "api/Customers" && method === "POST") {
    const req = body as CreateCustomerCommand;
    const c: Customer = { id: uid(), ...req };
    db.customers.push(c);
    saveDB(db);
    return delay(c as unknown) as Promise<T>;
  }
  const customerIdMatch = path.match(/^api\/Customers\/([^/]+)$/);
  if (customerIdMatch) {
    const id = customerIdMatch[1];
    const c = db.customers.find((x) => x.id === id);
    if (method === "GET") return delay(c as unknown) as Promise<T>;
    if (method === "PUT" && c) {
      const req = body as UpdateCustomerRequest;
      c.name = req.name;
      c.phone = req.phone;
      saveDB(db);
      return delay(c as unknown) as Promise<T>;
    }
    if (method === "DELETE") {
      db.customers = db.customers.filter((x) => x.id !== id);
      saveDB(db);
      return delay(undefined as unknown) as Promise<T>;
    }
  }

  // ---- Services ----
  if (path === "api/Services" && method === "GET") return delay(db.services as unknown) as Promise<T>;
  if (path === "api/Services" && method === "POST") {
    const req = body as CreateServiceCommand;
    const s: Service = { id: uid(), ...req };
    db.services.push(s);
    saveDB(db);
    return delay(s as unknown) as Promise<T>;
  }
  const svcIdMatch = path.match(/^api\/Services\/([^/]+)$/);
  if (svcIdMatch) {
    const id = svcIdMatch[1];
    const s = db.services.find((x) => x.id === id);
    if (method === "GET") return delay(s as unknown) as Promise<T>;
    if (method === "PUT" && s) {
      Object.assign(s, body as UpdateServiceRequest);
      saveDB(db);
      return delay(s as unknown) as Promise<T>;
    }
  }
  const svcHoursMatch = path.match(/^api\/Services\/([^/]+)\/available-hours$/);
  if (svcHoursMatch && method === "GET") {
    const dateISO = String(query?.date ?? new Date().toISOString().slice(0, 10));
    const workerId = (query?.workerId ?? undefined) as UUID | undefined;
    return delay(computeAvailableHours(db, svcHoursMatch[1], dateISO, workerId) as unknown) as Promise<T>;
  }

  // ---- Bundles ----
  if (path === "api/ServiceBundles" && method === "GET") return delay(db.bundles as unknown) as Promise<T>;
  if (path === "api/ServiceBundles" && method === "POST") {
    const req = body as CreateServiceBundleCommand;
    const b: ServiceBundle = { id: uid(), ...req };
    db.bundles.push(b);
    saveDB(db);
    return delay(b as unknown) as Promise<T>;
  }
  const bundleIdMatch = path.match(/^api\/ServiceBundles\/([^/]+)$/);
  if (bundleIdMatch) {
    const id = bundleIdMatch[1];
    const b = db.bundles.find((x) => x.id === id);
    if (method === "GET") return delay(b as unknown) as Promise<T>;
    if (method === "PUT" && b) {
      Object.assign(b, body as UpdateServiceBundleRequest);
      saveDB(db);
      return delay(b as unknown) as Promise<T>;
    }
  }

  // ---- Workers ----
  if (path === "api/Workers" && method === "GET") return delay(db.workers as unknown) as Promise<T>;
  if (path === "api/Workers" && method === "POST") {
    const req = body as CreateWorkerCommand;
    const w: Worker = { id: uid(), ...req };
    db.workers.push(w);
    db.availablePeriods[w.id] = [];
    db.unavailablePeriods[w.id] = [];
    saveDB(db);
    return delay(w as unknown) as Promise<T>;
  }
  const workerIdMatch = path.match(/^api\/Workers\/([^/]+)$/);
  if (workerIdMatch) {
    const id = workerIdMatch[1];
    const w = db.workers.find((x) => x.id === id);
    if (method === "GET") return delay(w as unknown) as Promise<T>;
    if (method === "PUT" && w) {
      const req = body as UpdateWorkerRequest;
      w.name = req.name;
      w.phone = req.phone;
      saveDB(db);
      return delay(w as unknown) as Promise<T>;
    }
  }
  const availMatch = path.match(/^api\/Workers\/([^/]+)\/available-periods$/);
  if (availMatch) {
    const id = availMatch[1];
    db.availablePeriods[id] ??= [];
    if (method === "GET") return delay(db.availablePeriods[id] as unknown) as Promise<T>;
    if (method === "POST") {
      const req = body as AddAvailablePeriodRequest;
      const p: AvailablePeriod = { id: uid(), ...req };
      db.availablePeriods[id].push(p);
      saveDB(db);
      return delay(p as unknown) as Promise<T>;
    }
    if (method === "DELETE") {
      const req = body as { id: UUID };
      db.availablePeriods[id] = db.availablePeriods[id].filter((p) => p.id !== req.id);
      saveDB(db);
      return delay(undefined as unknown) as Promise<T>;
    }
  }
  const unavailMatch = path.match(/^api\/Workers\/([^/]+)\/unavailable-periods$/);
  if (unavailMatch) {
    const id = unavailMatch[1];
    db.unavailablePeriods[id] ??= [];
    if (method === "POST") {
      const req = body as AddUnavailablePeriodRequest;
      const u: UnavailablePeriod = { id: uid(), ...req };
      db.unavailablePeriods[id].push(u);
      saveDB(db);
      return delay(u as unknown) as Promise<T>;
    }
    if (method === "DELETE") {
      const req = body as { id: UUID };
      db.unavailablePeriods[id] = db.unavailablePeriods[id].filter((u) => u.id !== req.id);
      saveDB(db);
      return delay(undefined as unknown) as Promise<T>;
    }
  }
  const preemptMatch = path.match(/^api\/Workers\/([^/]+)\/unavailable-periods\/preempt$/);
  if (preemptMatch && method === "POST") {
    const id = preemptMatch[1];
    db.unavailablePeriods[id] ??= [];
    const req = body as AddUnavailablePeriodRequest;
    const u: UnavailablePeriod = { id: uid(), ...req };
    db.unavailablePeriods[id].push(u);
    const us = new Date(req.start).getTime();
    const ue = new Date(req.end).getTime();
    for (const s of db.schedules) {
      if (s.workerId !== id) continue;
      const t = new Date(s.scheduledAt).getTime();
      if (t >= us && t < ue && s.status !== "Cancelled") s.status = "Cancelled";
    }
    saveDB(db);
    return delay(u as unknown) as Promise<T>;
  }

  // ---- Schedules ----
  if (path === "api/Schedules" && method === "GET") {
    let list = db.schedules;
    if (query?.customerId) list = list.filter((s) => s.customerId === query.customerId);
    if (query?.workerId) list = list.filter((s) => s.workerId === query.workerId);
    if (claims?.role === "Client" && claims.customerId) list = list.filter((s) => s.customerId === claims.customerId);
    if (claims?.role === "Worker" && claims.workerId) list = list.filter((s) => s.workerId === claims.workerId);
    return delay(list.slice().sort((a, b) => +new Date(a.scheduledAt) - +new Date(b.scheduledAt)) as unknown) as Promise<T>;
  }
  if (path === "api/Schedules" && method === "POST") {
    const cmd = body as CreateScheduleCommand;
    const s = buildSchedule(db, cmd);
    db.schedules.push(s);
    saveDB(db);
    return delay(s as unknown) as Promise<T>;
  }
  const schedIdMatch = path.match(/^api\/Schedules\/([^/]+)$/);
  if (schedIdMatch) {
    const id = schedIdMatch[1];
    const s = db.schedules.find((x) => x.id === id);
    if (method === "GET") return delay(s as unknown) as Promise<T>;
    if (method === "PUT" && s) {
      const req = body as UpdateScheduleRequest;
      s.scheduledAt = req.scheduledAt;
      s.duration = req.duration;
      s.services = req.serviceIds.map((sid) => {
        const svc = db.services.find((x) => x.id === sid);
        return { serviceId: sid, serviceName: svc?.name ?? "Serviço", value: svc?.value ?? 0, status: "Pending" };
      });
      const totals = computeScheduleTotals(db, req.serviceIds);
      s.grossValue = totals.gross;
      s.netValue = totals.net;
      saveDB(db);
      return delay(s as unknown) as Promise<T>;
    }
  }
  const confirmMatch = path.match(/^api\/Schedules\/([^/]+)\/confirm$/);
  if (confirmMatch && method === "PUT") {
    const s = db.schedules.find((x) => x.id === confirmMatch[1]);
    if (s) {
      s.status = "Confirmed";
      saveDB(db);
    }
    return delay(s as unknown) as Promise<T>;
  }
  const cancelMatch = path.match(/^api\/Schedules\/([^/]+)\/cancel$/);
  if (cancelMatch && method === "PUT") {
    const s = db.schedules.find((x) => x.id === cancelMatch[1]);
    if (s) {
      s.status = "Cancelled";
      saveDB(db);
    }
    return delay(s as unknown) as Promise<T>;
  }

  // ---- Admin schedules ----
  const adminSchedMatch = path.match(/^api\/admin\/schedules\/([^/]+)$/);
  if (adminSchedMatch && method === "PUT") {
    const id = adminSchedMatch[1];
    const idx = db.schedules.findIndex((x) => x.id === id);
    if (idx >= 0) {
      const req = body as AdminUpdateScheduleRequest;
      db.schedules[idx] = buildSchedule(db, { ...req, id }, { id, status: db.schedules[idx].status });
      saveDB(db);
      return delay(db.schedules[idx] as unknown) as Promise<T>;
    }
  }
  const adminLineMatch = path.match(/^api\/admin\/schedules\/([^/]+)\/services\/([^/]+)\/status$/);
  if (adminLineMatch && method === "PATCH") {
    const s = db.schedules.find((x) => x.id === adminLineMatch[1]);
    const line = s?.services.find((l) => l.serviceId === adminLineMatch[2]);
    if (line) {
      const req = body as UpdateServiceStatusRequest;
      line.status = req.status;
      saveDB(db);
    }
    return delay(s as unknown) as Promise<T>;
  }

  // ---- Dashboard ----
  if (path === "api/Dashboard/weekly-performance" && method === "GET") {
    const weekStart = String(query?.weekStart ?? new Date().toISOString());
    const workerId = (query?.workerId ?? (claims?.role === "Worker" ? claims.workerId : undefined)) as UUID | undefined;
    return delay(computeWeeklyPerformance(db, weekStart, workerId) as unknown) as Promise<T>;
  }

  throw new Error(`Mock: rota não implementada ${method} ${path}`);
}

export function resetMockDB() {
  if (typeof window === "undefined") return;
  window.localStorage.removeItem(KEY);
  window.localStorage.removeItem(TOKENS_KEY);
}
