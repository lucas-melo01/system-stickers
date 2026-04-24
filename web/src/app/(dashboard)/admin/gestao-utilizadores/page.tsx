import { redirect } from "next/navigation";
import { createClient } from "@/lib/supabase/server";
import { apiGet } from "@/lib/api";
import { GestaoUtilizadoresClient } from "./gestao-client";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";

export const dynamic = "force-dynamic";

type Sync = { perfil: string };
type U = { id: string; email: string; nome: string | null; perfil: string; ativo: boolean; criadoEm: string };

export default async function GestaoUtilizadoresPage() {
  const supabase = await createClient();
  if (!supabase) redirect("/login");
  const {
    data: { session },
  } = await supabase.auth.getSession();
  if (!session?.access_token) redirect("/login");
  if (!process.env.NEXT_PUBLIC_API_URL) redirect("/pedidos");
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
      <Typography variant="h5" color="primary" sx={{ fontWeight: 700 }} gutterBottom>
        Gestão de utilizadores
      </Typography>
      <Typography variant="body2" color="text.secondary" component="p" sx={{ mb: 2 }}>
        Apenas administradores. Crie contas (Supabase Auth), defina se são administrador ou operador, e ative ou
        inative o acesso à aplicação.
      </Typography>
      <GestaoUtilizadoresClient initial={list} />
    </Box>
  );
}
