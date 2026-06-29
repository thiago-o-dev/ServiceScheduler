import { api } from "./client";
import type {
  AddAvailablePeriodRequest,
  AddUnavailablePeriodRequest,
  AvailablePeriod,
  CreateWorkerCommand,
  PreemptUnavailablePeriodRequest,
  RemoveAvailablePeriodRequest,
  RemoveUnavailablePeriodRequest,
  UnavailablePeriod,
  UpdateWorkerRequest,
  UUID,
  Worker,
} from "./types";

export const workersApi = {
  list: () => api<Worker[]>("api/Workers"),
  get: (id: UUID) => api<Worker>(`api/Workers/${id}`),
  create: (req: CreateWorkerCommand) => api<Worker>("api/Workers", { method: "POST", body: req }),
  update: (id: UUID, req: UpdateWorkerRequest) =>
    api<Worker>(`api/Workers/${id}`, { method: "PUT", body: req }),

  listAvailable: (id: UUID) => api<AvailablePeriod[]>(`api/Workers/${id}/available-periods`),
  addAvailable: (id: UUID, req: AddAvailablePeriodRequest) =>
    api<AvailablePeriod>(`api/Workers/${id}/available-periods`, { method: "POST", body: req }),
  removeAvailable: (id: UUID, req: RemoveAvailablePeriodRequest) =>
    api<void>(`api/Workers/${id}/available-periods`, { method: "DELETE", body: req }),

  addUnavailable: (id: UUID, req: AddUnavailablePeriodRequest) =>
    api<UnavailablePeriod>(`api/Workers/${id}/unavailable-periods`, { method: "POST", body: req }),
  removeUnavailable: (id: UUID, req: RemoveUnavailablePeriodRequest) =>
    api<void>(`api/Workers/${id}/unavailable-periods`, { method: "DELETE", body: req }),
  preemptUnavailable: (id: UUID, req: PreemptUnavailablePeriodRequest) =>
    api<UnavailablePeriod>(`api/Workers/${id}/unavailable-periods/preempt`, {
      method: "POST",
      body: req,
    }),
};
