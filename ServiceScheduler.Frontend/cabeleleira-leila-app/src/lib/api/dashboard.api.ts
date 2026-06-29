import { api } from "./client";
import type { UUID, WeeklyPerformance } from "./types";

export const dashboardApi = {
  weekly: (weekStart: string, workerId?: UUID) =>
    api<WeeklyPerformance>("api/Dashboard/weekly-performance", {
      query: { weekStart, workerId },
    }),
};
