import { redirect } from "next/navigation";
import { createClient } from "@/lib/supabase/server";
import { apiGet } from "@/lib/api";
import { UsuariosClient } from "./usuarios-client";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";

export const dynamic = "force-dynamic";

type Sync = { perfil: string };
type U = { id: string; email: string; nome: string | null; perfil: string; ativo: boolean; criadoEm: string };

export default async function AdminUsuariosPage() {
  const supabase = await createClient();
  if (!supabase) redirect("/login");
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
    <Box>
      <Typography variant="h5" color="primary" gutterBottom sx={{ fontWeight: 700 }}>
        Usuários
      </Typography>
      <Typography variant="body2" color="text.secondary" component="p" sx={{ mb: 2 }}>
        Perfis vêm do banco da aplicação (sincronizados com Supabase). O primeiro acesso cria o registo em{" "}
        <Box component="code" sx={{ bgcolor: "grey.100", px: 0.5, borderRadius: 0.5 }}>
          /api/auth/sync
        </Box>
        .
      </Typography>
      <UsuariosClient initial={list} />
    </Box>
  );
}
