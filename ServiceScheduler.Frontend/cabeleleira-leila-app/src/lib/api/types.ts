// API types mirroring the OpenAPI schemas of the salon backend.
// Hand-maintained DTOs — keep in sync with core-api.json / gateway-api.json.

export type UUID = string;
export type ISODate = string; // e.g. "2026-06-29T14:00:00Z"
/** .NET TimeSpan, e.g. "01:30:00" */
export type TimeSpan = string;

export enum RegisterType {
  Client = 0,
  Worker = 1,
  Admin = 2,
}

export enum DayOfWeek {
  Sunday = 0,
  Monday = 1,
  Tuesday = 2,
  Wednesday = 3,
  Thursday = 4,
  Friday = 5,
  Saturday = 6,
}

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
  phone?: string;
  cpf?: string;
  registerType: RegisterType;
}

export interface TokenRequest {
  email: string;
  password: string;
}

export interface TokenResponse {
  token: string;
  expiresAt?: ISODate;
}

export interface Customer {
  id: UUID;
  name: string;
  email: string;
  phone: string;
  cpf: string;
}

export interface CreateCustomerCommand {
  name: string;
  email: string;
  phone: string;
  cpf: string;
}

export interface UpdateCustomerRequest {
  name: string;
  phone: string;
}

export interface Service {
  id: UUID;
  name: string;
  description: string;
  value: number;
  duration?: TimeSpan;
}

export interface CreateServiceCommand {
  name: string;
  description: string;
  value: number;
  duration?: TimeSpan;
}
export type UpdateServiceRequest = CreateServiceCommand;

export interface ServiceBundle {
  id: UUID;
  name: string;
  description: string;
  serviceIds: UUID[];
  discount: number;
}
export interface CreateServiceBundleCommand {
  name: string;
  description: string;
  serviceIds: UUID[];
  discount: number;
}
export type UpdateServiceBundleRequest = CreateServiceBundleCommand;

export interface Worker {
  id: UUID;
  name: string;
  email: string;
  phone: string;
  cpf: string;
  avatarUrl?: string;
  title?: string;
}
export interface CreateWorkerCommand {
  name: string;
  email: string;
  phone: string;
  cpf: string;
}
export interface UpdateWorkerRequest {
  name: string;
  phone: string;
}

export interface AvailablePeriod {
  id: UUID;
  dayOfWeek: DayOfWeek;
  startTime: string; // "HH:mm"
  endTime: string;
}
export interface AddAvailablePeriodRequest {
  dayOfWeek: DayOfWeek;
  startTime: string;
  endTime: string;
}
export interface RemoveAvailablePeriodRequest {
  id: UUID;
}

export interface UnavailablePeriod {
  id: UUID;
  start: ISODate;
  end: ISODate;
  reason: string | null;
}
export interface AddUnavailablePeriodRequest {
  start: ISODate;
  end: ISODate;
  reason: string | null;
}
export interface RemoveUnavailablePeriodRequest {
  id: UUID;
}
export interface PreemptUnavailablePeriodRequest {
  start: ISODate;
  end: ISODate;
  reason: string | null;
}

export type ScheduleStatus = "Pending" | "Confirmed" | "Cancelled" | "Completed";
export type ServiceLineStatus = "Pending" | "InProgress" | "Done" | "Cancelled";

export interface ScheduleService {
  serviceId: UUID;
  serviceName: string;
  value: number;
  status: ServiceLineStatus;
}
export interface Schedule {
  id: UUID;
  customerId: UUID;
  customerName: string;
  workerId: UUID;
  workerName: string;
  services: ScheduleService[];
  scheduledAt: ISODate;
  duration: TimeSpan;
  status: ScheduleStatus;
  grossValue: number;
  netValue: number;
}

export interface CreateScheduleCommand {
  customerId: UUID;
  workerId: UUID;
  serviceIds: UUID[];
  scheduledAt: ISODate;
  duration: TimeSpan;
}
export interface UpdateScheduleRequest {
  scheduledAt: ISODate;
  duration: TimeSpan;
  serviceIds: UUID[];
}
export interface AdminUpdateScheduleRequest {
  customerId: UUID;
  workerId: UUID;
  serviceIds: UUID[];
  scheduledAt: ISODate;
  duration: TimeSpan;
  overrideNetValue: number | null;
}
export interface UpdateServiceStatusRequest {
  status: ServiceLineStatus;
}

export interface AvailableHoursResponse {
  date: string; // YYYY-MM-DD
  slots: { start: ISODate; end: ISODate; workerId: UUID }[];
}

export interface WeeklyPerformance {
  weekStart: ISODate;
  weekEnd: ISODate;
  totals: {
    schedules: number;
    confirmed: number;
    cancelled: number;
    completed: number;
    grossRevenue: number;
    netRevenue: number;
    averageTicket: number;
  };
  byDay: { date: string; schedules: number; revenue: number }[];
  topServices: { serviceId: UUID; serviceName: string; count: number; revenue: number }[];
  topWorkers: { workerId: UUID; workerName: string; count: number; revenue: number }[];
}
