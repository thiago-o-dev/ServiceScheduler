import { api } from "./client";
import type { RegisterRequest, TokenRequest, TokenResponse } from "./types";

export const authApi = {
  token: (req: TokenRequest) =>
    api<TokenResponse>("api/Auth/token", { method: "POST", body: req, target: "gateway" }),
  register: (req: RegisterRequest) =>
    api<TokenResponse>("api/Auth/register", { method: "POST", body: req, target: "gateway" }),
};
