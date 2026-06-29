import { api } from "./client";
import type {
  AdminUpdateScheduleRequest,
  Schedule,
  UpdateServiceStatusRequest,
  UUID,
} from "./types";

export const adminApi = {
  updateSchedule: (id: UUID, req: AdminUpdateScheduleRequest) =>
    api<Schedule>(`api/admin/schedules/${id}`, { method: "PUT", body: req }),
  setServiceStatus: (scheduleId: UUID, serviceId: UUID, req: UpdateServiceStatusRequest) =>
    api<Schedule>(`api/admin/schedules/${scheduleId}/services/${serviceId}/status`, {
      method: "PATCH",
      body: req,
    }),
};
