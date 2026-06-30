import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { authApi } from "../api/auth.api";
import { getStoredToken, setStoredToken, api } from "../api/client";
import type { RegisterRequest, TokenRequest } from "../api/types";
import { decodeJwt, type JwtClaims, type Role } from "./jwt";

interface AuthContextValue {
  token: string | null;
  user: JwtClaims | null;
  role: Role | null;
  login: (req: TokenRequest) => Promise<JwtClaims | null>;
  register: (req: RegisterRequest) => Promise<JwtClaims | null>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [token, setToken] = useState<string | null>(null);
  const [user, setUser] = useState<JwtClaims | null>(null);

  useEffect(() => {
    setToken(getStoredToken());
  }, []);

  useEffect(() => {
    if (!token) {
      setUser(null);
      return;
    }

    const baseClaims = decodeJwt(token);
    if (!baseClaims) {
      setUser(null);
      return;
    }

    setUser(baseClaims);

    if (!baseClaims.customerId && !baseClaims.workerId) {
      const fetchProfileIds = async () => {
        try {
          if (baseClaims.role === "Client") {
            const searchRes = await api<{ items: { id: string; email: string }[] }>("api/Customers", {
              query: { searchTerm: baseClaims.email }
            });
            const matchingCustomer = searchRes?.items?.find(
              (c) => c.email.toLowerCase() === baseClaims.email.toLowerCase()
            );
            if (matchingCustomer) {
              setUser((prev) => prev && prev.sub === baseClaims.sub ? { ...prev, customerId: matchingCustomer.id } : prev);
            }
          } else if (baseClaims.role === "Worker") {
            const workers = await api<{ id: string; email: string }[]>("api/Workers");
            const matchingWorker = workers?.find(
              (w) => w.email.toLowerCase() === baseClaims.email.toLowerCase()
            );
            if (matchingWorker) {
              setUser((prev) => prev && prev.sub === baseClaims.sub ? { ...prev, workerId: matchingWorker.id } : prev);
            }
          }
        } catch (err) {
          console.error("Error fetching user profile ID from backend:", err);
        }
      };

      fetchProfileIds();
    }
  }, [token]);

  const value: AuthContextValue = {
    token,
    user,
    role: user?.role ?? null,
    login: async (req) => {
      const { token: t } = await authApi.token(req);
      setStoredToken(t);
      setToken(t);
      queryClient.clear();
      return decodeJwt(t);
    },
    register: async (req) => {
      const { token: t } = await authApi.register(req);
      setStoredToken(t);
      setToken(t);
      queryClient.clear();
      return decodeJwt(t);
    },
    logout: () => {
      setStoredToken(null);
      setToken(null);
      queryClient.clear();
    },
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside AuthProvider");
  return ctx;
}
