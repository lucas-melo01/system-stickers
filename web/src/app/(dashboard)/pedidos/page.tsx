import { redirect } from "next/navigation";
import { createClient } from "@/lib/supabase/server";
import { apiGet } from "@/lib/api";
import { PedidosView } from "./pedidos-view";
import Box from "@mui/material/Box";
import Alert from "@mui/material/Alert";
import Typography from "@mui/material/Typography";

export const dynamic = "force-dynamic";

type Row = {
  pedidoId: number;
  pedidoItemId: number;
  dataPedido: string;
  pedidoExternoId: string;
  nomeCliente: string;
  clienteCpf: string | null;
  produto: string;
  codigoFornecedor: string;
  cor: string | null;
  tamanho: string | null;
  quantidade: number;
  impresso: boolean;
};

type Paged = {
  items: Row[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
};

export default async function PedidosPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const sp = await searchParams;
  const q = typeof sp.q === "string" ? sp.q : undefined;
  const data = typeof sp.data === "string" ? sp.data : undefined;
  const page = typeof sp.page === "string" ? Math.max(1, parseInt(sp.page, 10) || 1) : 1;
  const supabase = await createClient();
  if (!supabase) redirect("/login");
  const {
    data: { session },
  } = await supabase.auth.getSession();
  if (!session?.access_token) redirect("/login");

  const qs = new URLSearchParams();
  if (q) qs.set("q", q);
  if (data) qs.set("data", data);
  qs.set("page", String(page));
  qs.set("pageSize", "50");
  const path = `/api/pedidos?${qs.toString()}`;

  let result: Paged;
  try {
    result = await apiGet<Paged>(path, session.access_token);
  } catch (e) {
    return (
      <Box>
        <Alert severity="error" sx={{ mb: 1 }}>
          Não foi possível carregar pedidos. A API precisa de JWT (Supabase) configurado.
        </Alert>
        <Typography variant="body2" color="text.secondary">
          {String(e)}
        </Typography>
      </Box>
    );
  }

  return (
    <PedidosView
      result={result}
      q={q}
      data={data}
      page={page}
    />
  );
}
