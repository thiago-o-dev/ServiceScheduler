export const brl = (n: number) =>
  n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });

export const dayName = (i: number) =>
  ["Domingo", "Segunda", "Terça", "Quarta", "Quinta", "Sexta", "Sábado"][i];

export const shortDay = (i: number) =>
  ["Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb"][i];
