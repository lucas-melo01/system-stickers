/** Base URL do backend (.NET) — usada nas rotas BFF (proxy). */
export function getBackendApiBase(): string {
  const base = (process.env.NEXT_PUBLIC_API_URL ?? "").trim().replace(/\/$/, "");
  if (!base) {
    throw new Error("Defina NEXT_PUBLIC_API_URL (URL da API no Render, sem barra final).");
  }
  return base;
}
