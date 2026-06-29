import type { UUID } from "../api/types";

export type Role = "Client" | "Worker" | "Admin";

export interface JwtClaims {
  sub: UUID;
  role: Role;
  name: string;
  email: string;
  customerId?: UUID;
  workerId?: UUID;
  exp?: number;
}

export function decodeJwt(token: string | null): JwtClaims | null {
  if (!token) return null;
  const parts = token.split(".");
  if (parts.length !== 3) return null;
  try {
    const json = atob(parts[1].replace(/-/g, "+").replace(/_/g, "/"));
    return JSON.parse(json) as JwtClaims;
  } catch {
    return null;
  }
}
