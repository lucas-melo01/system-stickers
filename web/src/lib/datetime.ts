// Helpers de data/hora para exibição na UI. O backend grava sempre em UTC
// (Postgres timestamptz) — aqui convertemos para o fuso de São Paulo
// respeitando DST histórico via Intl.DateTimeFormat.

const TZ = "America/Sao_Paulo";

// Converte um ISO 8601 (UTC vindo da API) para "dd/MM/yyyy" em horário
// de Brasília. Devolve "" se o input for vazio/invalido.
export function formatDataBR(iso?: string | null): string {
  if (!iso) return "";
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return "";
  return new Intl.DateTimeFormat("pt-BR", {
    timeZone: TZ,
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(d);
}

// Versão com hora "dd/MM/yyyy HH:mm".
export function formatDataHoraBR(iso?: string | null): string {
  if (!iso) return "";
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return "";
  return new Intl.DateTimeFormat("pt-BR", {
    timeZone: TZ,
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(d);
}
