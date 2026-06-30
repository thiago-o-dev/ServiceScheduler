// HTTP client for the salon backend.
// Defaults to a fully-functional in-memory MOCK so the preview works without the real API.
// Set VITE_USE_MOCK_API=false and provide VITE_GATEWAY_URL / VITE_CORE_API_URL to hit your real backend.

import { mockHandle } from "./mock";

const GATEWAY_URL = import.meta.env.VITE_GATEWAY_URL ?? "https://localhost:7080";
const CORE_URL =
  import.meta.env.VITE_CORE_API_URL ?? `${GATEWAY_URL}/servicescheduler-api`;
const USE_MOCK =
  (import.meta.env.VITE_USE_MOCK_API ?? "true").toString().toLowerCase() !== "false";

const AUTH_KEY = "leila.auth.token";

export function getStoredToken(): string | null {
  if (typeof window === "undefined") return null;
  return window.localStorage.getItem(AUTH_KEY);
}
export function setStoredToken(token: string | null) {
  if (typeof window === "undefined") return;
  if (token) window.localStorage.setItem(AUTH_KEY, token);
  else window.localStorage.removeItem(AUTH_KEY);
}

export class ApiError extends Error {
  constructor(public status: number, message: string, public payload?: unknown) {
    super(message);
  }
}

type Target = "gateway" | "core";
interface RequestOpts {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  body?: unknown;
  query?: Record<string, string | number | string[] | undefined | null>;
  target?: Target;
  signal?: AbortSignal;
}

export async function api<T = unknown>(path: string, opts: RequestOpts = {}): Promise<T> {
  const { method = "GET", body, query, target = "core", signal } = opts;
  const token = getStoredToken();

  if (USE_MOCK) {
    return mockHandle<T>({ path, method, body, query, target, token });
  }

  const base = target === "gateway" ? GATEWAY_URL : CORE_URL;
  const url = new URL(path, base.endsWith("/") ? base : base + "/");
  if (query) {
  for (const [k, v] of Object.entries(query)) {
    if (v == null) continue;
    if (Array.isArray(v)) {
      for (const item of v) url.searchParams.append(k, String(item));
    } else {
      url.searchParams.set(k, String(v));
    }
  }
}
  const res = await fetch(url.toString().replace(/\/$/, ""), {
    method,
    signal,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: body != null ? JSON.stringify(body) : undefined,
  });
  if (!res.ok) {
    let payload: unknown = undefined;
    try {
      payload = await res.json();
    } catch {
      /* ignore */
    }

    let message = `${method} ${path} → ${res.status}`;

    if (typeof payload === "object" && payload !== null) {
      if ("errorMessage" in payload) {
        message = String((payload as { errorMessage: string }).errorMessage);
      } else if ("detail" in payload) {
        message = String((payload as { detail: string }).detail);
      }
    }

    throw new ApiError(res.status, message, payload);
  }
  if (res.status === 204) return undefined as T;
  const txt = await res.text();
  return (txt ? JSON.parse(txt) : undefined) as T;
}

export const apiConfig = { GATEWAY_URL, CORE_URL, USE_MOCK };
