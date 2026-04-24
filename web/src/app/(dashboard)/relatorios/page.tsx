import { redirect } from "next/navigation";
import { createClient } from "@/lib/supabase/server";
import { apiGet } from "@/lib/api";
import { ExportExcelButton } from "./export-button";

export const dynamic = "force-dynamic";

type Venda = {
  dataPedido: string;
  sku: string;
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

  return (
    <div>
      <h1 className="text-2xl font-bold text-[#FFF200] mb-4">Relatórios de vendas</h1>
      <form className="mb-6 flex flex-wrap gap-2 items-end">
        <div>
          <label className="text-xs text-zinc-500 block">Início</label>
          <input name="inicio" type="date" defaultValue={inicio} className="px-2 py-1 rounded bg-zinc-800 border border-zinc-600" required />
        </div>
        <div>
          <label className="text-xs text-zinc-500 block">Fim</label>
          <input name="fim" type="date" defaultValue={fim} className="px-2 py-1 rounded bg-zinc-800 border border-zinc-600" required />
        </div>
        <button type="submit" className="px-3 py-1.5 bg-[#001623] text-[#FFF200] text-sm font-semibold rounded border border-zinc-600">
          Aplicar
        </button>
      </form>
      {err && <p className="text-red-400 mb-2">{err}</p>}
      {inicio && fim && rows.length > 0 && <ExportExcelButton inicio={inicio} fim={fim} />}

      <div className="overflow-x-auto border border-zinc-800 rounded-lg">
        <table className="w-full text-sm">
          <thead>
            <tr className="bg-zinc-900 text-left text-zinc-400">
              <th className="p-2">Data</th>
              <th className="p-2">SKU</th>
              <th className="p-2">Vendedor</th>
              <th className="p-2">Peça</th>
              <th className="p-2">Cliente</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r, i) => (
              <tr key={i} className="border-t border-zinc-800">
                <td className="p-2 whitespace-nowrap">{r.dataPedido?.slice(0, 10)}</td>
                <td className="p-2">{r.sku}</td>
                <td className="p-2">{r.vendedor}</td>
                <td className="p-2 max-w-[200px] truncate" title={r.peca}>
                  {r.peca}
                </td>
                <td className="p-2">{r.cliente}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
