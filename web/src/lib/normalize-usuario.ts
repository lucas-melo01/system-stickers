/** Resposta GET /api/admin/usuarios — .NET pode serializar em PascalCase. */
export type UsuarioLista = {
  id: string;
  email: string;
  nome: string | null;
  perfil: string | number;
  ativo: boolean;
  criadoEm: string;
};

export function normalizeUsuarioLista(raw: unknown): UsuarioLista[] {
  if (!Array.isArray(raw)) return [];
  return raw.map((x) => {
    const o = x as Record<string, unknown>;
    return {
      id: String(o.id ?? o.Id ?? ""),
      email: String(o.email ?? o.Email ?? ""),
      nome: (o.nome ?? o.Nome ?? null) as string | null,
      perfil: (o.perfil ?? o.Perfil ?? "Operador") as string | number,
      ativo: Boolean(o.ativo ?? o.Ativo ?? false),
      criadoEm: String(o.criadoEm ?? o.CriadoEm ?? ""),
    };
  });
}
