import { redirect } from "next/navigation";
import { createClient } from "@/lib/supabase/server";
import { apiGet } from "@/lib/api";
import { normalizePaged, normalizeProduto } from "@/lib/normalize-cadastros";
import { ProdutosView } from "./produtos-view";
import Box from "@mui/material/Box";
import Alert from "@mui/material/Alert";

export const dynamic = "force-dynamic";

export default async function ProdutosPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const sp = await searchParams;
  const q = typeof sp.q === "string" ? sp.q : undefined;
  const loja = typeof sp.loja === "string" ? sp.loja : undefined;
  const semFornecedor = sp.semFornecedor === "1";
  const page = typeof sp.page === "string" ? Math.max(1, parseInt(sp.page, 10) || 1) : 1;

  const supabase = await createClient();
  if (!supabase) redirect("/login");
  const {
    data: { session },
  } = await supabase.auth.getSession();
  if (!session?.access_token) redirect("/login");

  const qs = new URLSearchParams();
  if (q) qs.set("q", q);
  if (loja) qs.set("loja", loja);
  if (semFornecedor) qs.set("semFornecedor", "true");
  qs.set("page", String(page));
  qs.set("pageSize", "50");

  try {
    const raw = await apiGet<unknown>(`/api/produtos?${qs.toString()}`, session.access_token);
    const data = normalizePaged(raw, normalizeProduto);
    return <ProdutosView initial={data} q={q} loja={loja} semFornecedor={semFornecedor} page={page} />;
  } catch (e) {
    return (
      <Box>
        <Alert severity="error">Erro ao carregar produtos: {String(e)}</Alert>
      </Box>
    );
  }
}
