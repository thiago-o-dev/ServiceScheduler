import { api } from "./client";
import type {
  AvailableHoursResponse,
  CreateServiceCommand,
  Service,
  UpdateServiceRequest,
  UUID,
} from "./types";

export const servicesApi = {
  list: () => api<Service[]>("api/Services"),
  get: (id: UUID) => api<Service>(`api/Services/${id}`),
  create: (req: CreateServiceCommand) => api<Service>("api/Services", { method: "POST", body: req }),
  update: (id: UUID, req: UpdateServiceRequest) =>
    api<Service>(`api/Services/${id}`, { method: "PUT", body: req }),
  availableHours: (id: UUID, date: string, workerId?: UUID) =>
    api<AvailableHoursResponse>(`api/Services/${id}/available-hours`, {
      query: { date, workerId },
    }),
};
