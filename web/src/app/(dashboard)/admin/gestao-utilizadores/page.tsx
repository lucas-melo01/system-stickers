import { headers } from "next/headers";
import { redirect } from "next/navigation";
import { createClient } from "@/lib/supabase/server";
import { ApiError } from "@/lib/api";
import { GestaoUtilizadoresClient } from "./gestao-client";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import Alert from "@mui/material/Alert";

export const dynamic = "force-dynamic";

type U = { id: string; email: string; nome: string | null; perfil: string | number; ativo: boolean; criadoEm: string };

// Lista via BFF same-origin (alinhado com PATCH /api/admin/usuarios/:id).
async function fetchListaUtilizadores(accessToken: string): Promise<U[]> {
  const h = await headers();
  const host = h.get("x-forwarded-host") ?? h.get("host") ?? "localhost:3000";
  const proto =
    h.get("x-forwarded-proto") ?? (host.startsWith("localhost") || host.startsWith("127.") ? "http" : "https");
  const url = `${proto}://${host}/api/admin/usuarios`;
  const r = await fetch(url, {
    headers: {
      Accept: "application/json",
      Authorization: `Bearer ${accessToken}`,
    },
    cache: "no-store",
  });
  if (!r.ok) {
    const t = await r.text();
    throw new ApiError(r.status, t || r.statusText);
  }
  return r.json() as Promise<U[]>;
}

export default async function GestaoUtilizadoresPage() {
  const supabase = await createClient();
  if (!supabase) redirect("/login");
  const {
    data: { session },
  } = await supabase.auth.getSession();
  if (!session?.access_token) redirect("/login");

  let list: U[] = [];
  let erro: string | null = null;
  try {
    list = await fetchListaUtilizadores(session.access_token);
  } catch (e) {
    if (e instanceof ApiError) {
      switch (e.status) {
        case 401:
          erro = "Sessão expirada. Faça login novamente.";
          break;
        case 403:
          erro = "Esta área só é visível para administradores. As suas permissões são insuficientes para listar ou alterar utilizadores.";
          break;
        case 404:
          erro = "A API ainda não tem as rotas de administração. Faça o redeploy da API para obter a versão mais recente.";
          break;
        case 500:
          erro = `Falha no proxy para a API (HTTP 500). Defina NEXT_PUBLIC_API_URL (URL do backend) no Vercel. Detalhe: ${e.body || "—"}`;
          break;
        case 501:
          erro = "A API está sem autenticação configurada. Defina SUPABASE_URL e SUPABASE_JWT_SECRET no host da API e faça redeploy.";
          break;
        default:
          erro = `Falha a carregar a lista (HTTP ${e.status}): ${e.body || "sem detalhe"}`;
      }
    } else {
      erro = `Falha a carregar a lista: ${String(e)}`;
    }
  }

  return (
    <Box>
      <Typography variant="h5" color="primary" sx={{ fontWeight: 700 }} gutterBottom>
        Gestão de utilizadores
      </Typography>
      <Typography variant="body2" color="text.secondary" component="p" sx={{ mb: 2 }}>
        Listagem dos utilizadores do sistema. A criação de contas e as alterações de perfil/estado continuam restritas a
        administradores do lado da API (podem devolver &ldquo;sem permissão&rdquo;).
      </Typography>
      {erro && (
        <Alert severity="info" sx={{ mb: 2 }}>
          {erro}
        </Alert>
      )}
      <GestaoUtilizadoresClient initial={list} />
    </Box>
  );
}
