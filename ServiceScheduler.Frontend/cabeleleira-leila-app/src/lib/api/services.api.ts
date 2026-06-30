import { api } from "./client";
import type {
  WorkerAvailabilityMap,
  CreateServiceCommand,
  Service,
  UpdateServiceRequest,
  UUID,
} from "./types";

function toLocalIso(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(
    date.getMinutes(),
  )}:${pad(date.getSeconds())}`;
}

export const servicesApi = {
  list: () => api<Service[]>("api/Services"),

  get: (id: UUID) => api<Service>(`api/Services/${id}`),

  create: (req: CreateServiceCommand) =>
    api<Service>("api/Services", { method: "POST", body: req }),

  update: (id: UUID, req: UpdateServiceRequest) =>
    api<Service>(`api/Services/${id}`, { method: "PUT", body: req }),

  // serviceIds can be 1..N services; the day is expanded to [00:00, +24h)
  // to match the new start/end range on the backend.
  availableHours: (serviceIds: UUID[], date: string, workerId?: UUID) => {
    const start = new Date(`${date}T00:00:00`);
    const end = new Date(start);
    end.setDate(end.getDate() + 1);

    return api<WorkerAvailabilityMap>("api/Services/available-hours", {
      query: { serviceIds, start: toLocalIso(start), end: toLocalIso(end), workerId },
    });
  },
};