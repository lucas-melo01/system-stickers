import { redirect } from "next/navigation";
import { createClient } from "@/lib/supabase/server";
import { apiGet } from "@/lib/api";
import { UsuariosClient } from "./usuarios-client";

export const dynamic = "force-dynamic";

type Sync = { perfil: string };
type U = { id: string; email: string; nome: string | null; perfil: string; ativo: boolean; criadoEm: string };

export default async function AdminUsuariosPage() {
  const supabase = await createClient();
  const {
    data: { session },
  } = await supabase.auth.getSession();
  if (!session?.access_token) redirect("/login");
  const api = process.env.NEXT_PUBLIC_API_URL;
  if (!api) redirect("/pedidos");
  let me: Sync;
  try {
    me = await apiGet<Sync>("/api/auth/sync", session.access_token);
  } catch {
    redirect("/pedidos");
  }
  if (me.perfil !== "Admin") redirect("/pedidos");

  let list: U[] = [];
  try {
    list = await apiGet<U[]>("/api/admin/usuarios", session.access_token);
  } catch {
    list = [];
  }

  return (
    <div>
      <h1 className="text-2xl font-bold text-[#FFF200] mb-2">Usuários</h1>
      <p className="text-zinc-500 text-sm mb-4">
        Perfis vêm do banco da aplicação (sincronizados com Supabase). O primeiro acesso cria o registro em{" "}
        <code className="text-zinc-400">/api/auth/sync</code>.
      </p>
      <UsuariosClient initial={list} />
    </div>
  );
}
