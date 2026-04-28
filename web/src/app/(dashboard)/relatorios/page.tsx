import { redirect } from "next/navigation";
import { createClient } from "@/lib/supabase/server";
import { apiGet } from "@/lib/api";
import { RelatoriosView } from "./relatorios-view";

export const dynamic = "force-dynamic";

type Venda = {
  dataPedido: string;
  codigoFornecedor: string;
  vendedor: string;
  peca: string;
  cliente: string;
  tipoEnvio: string;
  valorCusto: number;
  valorVenda: number;
  formaPagamento: string;
  valorFrete: number;
};

export default async function RelatoriosPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const sp = await searchParams;
  const inicio = typeof sp.inicio === "string" ? sp.inicio : undefined;
  const fim = typeof sp.fim === "string" ? sp.fim : undefined;

  const supabase = await createClient();
  if (!supabase) redirect("/login");
  const {
    data: { session },
  } = await supabase.auth.getSession();
  if (!session?.access_token) redirect("/login");

  let rows: Venda[] = [];
  let err: string | null = null;
  if (inicio && fim) {
    try {
      const qs = new URLSearchParams({ inicio, fim });
      rows = await apiGet<Venda[]>(`/api/relatorios/vendas?${qs}`, session.access_token);
    } catch (e) {
      err = String(e);
    }
  }

  return <RelatoriosView inicio={inicio} fim={fim} rows={rows} err={err} />;
}
