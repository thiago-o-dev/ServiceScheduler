// Helpers for .NET TimeSpan strings "HH:MM:SS"

export function minutesToTimeSpan(totalMinutes: number): string {
  const h = Math.floor(totalMinutes / 60);
  const m = totalMinutes % 60;
  return `${String(h).padStart(2, "0")}:${String(m).padStart(2, "0")}:00`;
}

export function timeSpanToMinutes(ts: string): number {
  const [h, m] = ts.split(":").map(Number);
  return (h || 0) * 60 + (m || 0);
}

export function formatDurationLabel(totalMinutes: number): string {
  const h = Math.floor(totalMinutes / 60);
  const m = totalMinutes % 60;
  if (h && m) return `${h}h${m}`;
  if (h) return `${h}h`;
  return `${m}min`;
}
