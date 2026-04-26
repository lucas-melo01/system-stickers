import { redirect } from "next/navigation";
import { createClient } from "@/lib/supabase/server";
import { fetchPerfilAtual } from "@/lib/auth-sync";
import { isAdminPerfil } from "@/lib/is-admin-perfil";
import { getBackendApiBase } from "@/lib/server-api-base";
import { ApiError } from "@/lib/api";
import { normalizeUsuarioLista, type UsuarioLista } from "@/lib/normalize-usuario";
import { GestaoUtilizadoresClient } from "./gestao-client";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import Alert from "@mui/material/Alert";

export const dynamic = "force-dynamic";

// Listagem directa ao backend .NET (evita fetch ao próprio host no Vercel RSC,
// que por vezes falha ou devolve lista vazia). Normaliza PascalCase/camelCase.
async function fetchListaUtilizadores(accessToken: string): Promise<UsuarioLista[]> {
  const base = getBackendApiBase();
  const r = await fetch(`${base}/api/admin/usuarios`, {
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
  const json: unknown = await r.json();
  return normalizeUsuarioLista(json);
}

export default async function GestaoUtilizadoresPage() {
  const supabase = await createClient();
  if (!supabase) redirect("/login");
  const {
    data: { session },
  } = await supabase.auth.getSession();
  if (!session?.access_token) redirect("/login");

  const me = await fetchPerfilAtual(session.access_token);
  if (!isAdminPerfil(me?.perfil)) {
    redirect("/pedidos");
  }

  let list: UsuarioLista[] = [];
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
          erro = `Falha ao contactar a API (HTTP 500). Defina NEXT_PUBLIC_API_URL (URL do backend) no Vercel. Detalhe: ${e.body || "—"}`;
          break;
        case 501:
          erro = "A API está sem autenticação configurada. Defina SUPABASE_URL e SUPABASE_JWT_SECRET no host da API e faça redeploy.";
          break;
        default:
          erro = `Falha a carregar a lista (HTTP ${e.status}): ${e.body || "sem detalhe"}`;
      }
    } else if (e instanceof Error && e.message.includes("Defina NEXT_PUBLIC_API_URL")) {
      erro = "API não configurada: defina NEXT_PUBLIC_API_URL no Vercel (URL do backend .NET, sem barra no fim) e redeploy.";
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
