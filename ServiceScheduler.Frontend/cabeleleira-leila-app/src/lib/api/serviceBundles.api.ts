import { api } from "./client";
import type {
  CreateServiceBundleCommand,
  ServiceBundle,
  UpdateServiceBundleRequest,
  UUID,
} from "./types";

export const bundlesApi = {
  list: () => api<ServiceBundle[]>("api/ServiceBundles"),
  get: (id: UUID) => api<ServiceBundle>(`api/ServiceBundles/${id}`),
  create: (req: CreateServiceBundleCommand) =>
    api<ServiceBundle>("api/ServiceBundles", { method: "POST", body: req }),
  update: (id: UUID, req: UpdateServiceBundleRequest) =>
    api<ServiceBundle>(`api/ServiceBundles/${id}`, { method: "PUT", body: req }),
};
