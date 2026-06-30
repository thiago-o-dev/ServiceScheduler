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
    const raw = JSON.parse(json);
    
    let role: Role = "Client";
    if (raw.role) {
      role = raw.role;
    } else {
      const realmRoles: string[] = raw.realm_access?.roles ?? [];
      if (realmRoles.includes("admin")) {
        role = "Admin";
      } else if (realmRoles.includes("worker")) {
        role = "Worker";
      } else if (realmRoles.includes("customer")) {
        role = "Client";
      }
    }

    return {
      sub: raw.sub,
      role,
      name: raw.name ?? raw.preferred_username ?? raw.email ?? "User",
      email: raw.email ?? raw.preferred_username ?? "",
      customerId: raw.customerId,
      workerId: raw.workerId,
      exp: raw.exp,
    };
  } catch {
    return null;
  }
}
