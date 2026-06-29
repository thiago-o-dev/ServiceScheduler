import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { authApi } from "../api/auth.api";
import { getStoredToken, setStoredToken } from "../api/client";
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

  useEffect(() => {
    setToken(getStoredToken());
  }, []);

  const user = useMemo(() => decodeJwt(token), [token]);

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
