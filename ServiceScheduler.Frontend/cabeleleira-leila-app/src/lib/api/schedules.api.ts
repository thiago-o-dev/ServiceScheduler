import { api } from "./client";
import type {
  CreateScheduleCommand,
  Schedule,
  UpdateScheduleRequest,
  UUID,
} from "./types";

export const schedulesApi = {
  list: (filters?: { customerId?: UUID; workerId?: UUID }) =>
    api<Schedule[]>("api/Schedules", { query: filters }),
  get: (id: UUID) => api<Schedule>(`api/Schedules/${id}`),
  create: (req: CreateScheduleCommand) =>
    api<Schedule>("api/Schedules", { method: "POST", body: req }),
  update: (id: UUID, req: UpdateScheduleRequest) =>
    api<Schedule>(`api/Schedules/${id}`, { method: "PUT", body: req }),
  confirm: (id: UUID) => api<Schedule>(`api/Schedules/${id}/confirm`, { method: "PUT" }),
  cancel: (id: UUID) => api<Schedule>(`api/Schedules/${id}/cancel`, { method: "PUT" }),
};
