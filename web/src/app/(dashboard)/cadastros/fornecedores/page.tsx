import { redirect } from "next/navigation";
import { createClient } from "@/lib/supabase/server";
import { apiGet } from "@/lib/api";
import { normalizeFornecedor, normalizePaged } from "@/lib/normalize-cadastros";
import { FornecedoresClient } from "./fornecedores-client";
import Box from "@mui/material/Box";
import Alert from "@mui/material/Alert";

export const dynamic = "force-dynamic";

export default async function FornecedoresPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const sp = await searchParams;
  const q = typeof sp.q === "string" ? sp.q : undefined;
  const page = typeof sp.page === "string" ? Math.max(1, parseInt(sp.page, 10) || 1) : 1;

  const supabase = await createClient();
  if (!supabase) redirect("/login");
  const {
    data: { session },
  } = await supabase.auth.getSession();
  if (!session?.access_token) redirect("/login");

  const qs = new URLSearchParams();
  if (q) qs.set("q", q);
  qs.set("page", String(page));
  qs.set("pageSize", "50");

  try {
    const raw = await apiGet<unknown>(`/api/fornecedores?${qs.toString()}`, session.access_token);
    const data = normalizePaged(raw, normalizeFornecedor);
    return <FornecedoresClient initial={data} q={q} page={page} />;
  } catch (e) {
    return (
      <Box>
        <Alert severity="error">Erro ao carregar fornecedores: {String(e)}</Alert>
      </Box>
    );
  }
}
