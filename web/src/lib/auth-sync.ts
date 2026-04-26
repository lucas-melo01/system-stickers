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

/** Alinha com a API .NET: PascalCase (Id, Perfil) e `Perfil` como enum (0/1) ou "Admin"/"Operador". */
export function normalizePerfilAtualResponse(raw: unknown): PerfilAtual | null {
  if (raw === null || typeof raw !== "object") return null;
  const o = raw as Record<string, unknown>;
  const id = String(o.id ?? o.Id ?? "");
  const email = String(o.email ?? o.Email ?? "");
  if (!id && !email) return null;
  const perfilBruto = o.perfil ?? o.Perfil;
  let perfil: string | number;
  if (typeof perfilBruto === "number" && !Number.isNaN(perfilBruto)) {
    perfil = perfilBruto;
  } else if (typeof perfilBruto === "string") {
    const t = perfilBruto.trim();
    const n = Number(t);
    if (t !== "" && !Number.isNaN(n) && String(n) === t) {
      perfil = n;
    } else {
      perfil = t;
    }
  } else {
    perfil = "Operador";
  }
  const ativoV = o.ativo ?? o.Ativo;
  const ativo = typeof ativoV === "boolean" ? ativoV : ativoV == null ? undefined : Boolean(ativoV);
  return {
    id,
    email,
    nome: (o.nome ?? o.Nome ?? null) as string | null,
    perfil,
    ativo,
  };
}

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
    const json: unknown = await r.json();
    return normalizePerfilAtualResponse(json);
  } catch (e) {
    console.error("fetchPerfilAtual", e);
    return null;
  }
}
