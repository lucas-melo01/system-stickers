/**
 * Obtém o utilizador actual (incluindo perfil) a partir de `/api/auth/sync`.
 * Tenta GET; se a API estiver numa versão antiga (405 Method Not Allowed),
 * faz fallback para POST.
 */
export type PerfilAtual = {
  id: string;
  email: string;
  nome?: string | null;
  perfil: string | number;
  ativo?: boolean;
};

const base = process.env.NEXT_PUBLIC_API_URL ?? "";

export async function fetchPerfilAtual(accessToken: string): Promise<PerfilAtual | null> {
  if (!base) return null;

  const url = `${base.replace(/\/$/, "")}/api/auth/sync`;
  const headers = {
    Accept: "application/json",
    Authorization: `Bearer ${accessToken}`,
  } as const;

  try {
    let r = await fetch(url, { method: "GET", headers, cache: "no-store" });
    if (r.status === 405 || r.status === 404) {
      r = await fetch(url, { method: "POST", headers, cache: "no-store" });
    }
    if (!r.ok) {
      console.error("fetchPerfilAtual: resposta", r.status, await r.text().catch(() => ""));
      return null;
    }
    return (await r.json()) as PerfilAtual;
  } catch (e) {
    console.error("fetchPerfilAtual", e);
    return null;
  }
}
