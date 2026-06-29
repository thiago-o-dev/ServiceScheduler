// ISO week helpers (Monday start).
import { addDays, startOfWeek, endOfWeek, format } from "date-fns";

export function getWeekStart(d: Date): Date {
  return startOfWeek(d, { weekStartsOn: 1 });
}
export function getWeekEnd(d: Date): Date {
  return endOfWeek(d, { weekStartsOn: 1 });
}
export function weekDays(start: Date): Date[] {
  return Array.from({ length: 7 }, (_, i) => addDays(start, i));
}
export function formatWeekRange(start: Date): string {
  const end = addDays(start, 6);
  return `${format(start, "dd 'de' MMM")} – ${format(end, "dd 'de' MMM, yyyy")}`;
}
export function isoDay(d: Date): string {
  return format(d, "yyyy-MM-dd");
}
