import { redirect } from "next/navigation";
import { createClient } from "@/lib/supabase/server";
import { apiGet } from "@/lib/api";
import { normalizeNotificacao, normalizePaged } from "@/lib/normalize-cadastros";
import { PedidoCompraView } from "./pedido-compra-view";
import Box from "@mui/material/Box";
import Alert from "@mui/material/Alert";

export const dynamic = "force-dynamic";

export default async function PedidoCompraPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const sp = await searchParams;
  const data = typeof sp.data === "string" ? sp.data : undefined;
  const status = typeof sp.status === "string" ? sp.status : undefined;
  const pedido = typeof sp.pedido === "string" ? sp.pedido : undefined;
  const page = typeof sp.page === "string" ? Math.max(1, parseInt(sp.page, 10) || 1) : 1;

  const supabase = await createClient();
  if (!supabase) redirect("/login");
  const {
    data: { session },
  } = await supabase.auth.getSession();
  if (!session?.access_token) redirect("/login");

  const qs = new URLSearchParams();
  if (data) qs.set("data", data);
  if (status) qs.set("status", status);
  if (pedido) qs.set("pedido", pedido);
  qs.set("page", String(page));
  qs.set("pageSize", "50");

  try {
    const raw = await apiGet<unknown>(`/api/notificacoes/pedido-compra?${qs.toString()}`, session.access_token);
    const result = normalizePaged(raw, normalizeNotificacao);
    return <PedidoCompraView initial={result} data={data} status={status} pedido={pedido} page={page} />;
  } catch (e) {
    return (
      <Box>
        <Alert severity="error">Erro ao carregar notificações: {String(e)}</Alert>
      </Box>
    );
  }
}
