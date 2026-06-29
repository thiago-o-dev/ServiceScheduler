import { redirect } from "@tanstack/react-router";
import type { Role } from "@/lib/auth/jwt";
import { decodeJwt } from "@/lib/auth/jwt";
import { getStoredToken } from "@/lib/api/client";

export function guardRole(required: Role) {
  return () => {
    if (typeof window === "undefined") return;
    const claims = decodeJwt(getStoredToken());
    if (!claims) throw redirect({ to: "/auth/login" });
    if (claims.role !== required) throw redirect({ to: "/" });
  };
}
