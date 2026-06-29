import { api } from "./client";
import type { CreateCustomerCommand, Customer, UpdateCustomerRequest, UUID } from "./types";

export const customersApi = {
  list: () => api<Customer[]>("api/Customers"),
  get: (id: UUID) => api<Customer>(`api/Customers/${id}`),
  create: (req: CreateCustomerCommand) => api<Customer>("api/Customers", { method: "POST", body: req }),
  update: (id: UUID, req: UpdateCustomerRequest) =>
    api<Customer>(`api/Customers/${id}`, { method: "PUT", body: req }),
  remove: (id: UUID) => api<void>(`api/Customers/${id}`, { method: "DELETE" }),
};
