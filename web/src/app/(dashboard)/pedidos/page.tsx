import Link from "next/link";
import { redirect } from "next/navigation";
import { createClient } from "@/lib/supabase/server";
import { apiGet } from "@/lib/api";
import { PedidoRowActions } from "./row-actions";

export const dynamic = "force-dynamic";

type Row = {
  pedidoId: number;
  pedidoItemId: number;
  dataPedido: string;
  pedidoExternoId: string;
  nomeCliente: string;
  clienteCpf: string | null;
  produto: string;
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
  qs.set("pageSize", "15");
  const path = `/api/pedidos?${qs.toString()}`;

  let result: Paged;
  try {
    result = await apiGet<Paged>(path, session.access_token);
  } catch (e) {
    return (
      <div>
        <p className="text-red-400">Não foi possível carregar pedidos. A API exige variáveis Supabase (JWT) na API.</p>
        <p className="text-zinc-500 text-sm mt-2">{String(e)}</p>
      </div>
    );
  }

  return (
    <div>
      <h1 className="text-2xl font-bold text-[#FFF200] mb-4">Pedidos</h1>
      <form className="mb-4 flex flex-wrap gap-2 items-end">
        <div>
          <label className="text-xs text-zinc-500 block">Busca</label>
          <input
            name="q"
            defaultValue={q}
            placeholder="ID, nome, CPF"
            className="px-2 py-1 rounded bg-zinc-800 border border-zinc-600 text-sm"
          />
        </div>
        <div>
          <label className="text-xs text-zinc-500 block">Data</label>
          <input
            name="data"
            type="date"
            defaultValue={data}
            className="px-2 py-1 rounded bg-zinc-800 border border-zinc-600 text-sm"
          />
        </div>
        <button type="submit" className="px-3 py-1.5 bg-[#001623] text-[#FFF200] text-sm font-semibold rounded border border-zinc-600">
          Filtrar
        </button>
        <Link href="/pedidos" className="text-sm text-zinc-400">
          limpar
        </Link>
      </form>
      <div className="overflow-x-auto border border-zinc-800 rounded-lg">
        <table className="w-full text-sm">
          <thead>
            <tr className="bg-zinc-900 text-left text-zinc-400">
              <th className="p-2">Data</th>
              <th className="p-2">Pedido</th>
              <th className="p-2">Cliente</th>
              <th className="p-2">Item</th>
              <th className="p-2">Qtd</th>
              <th className="p-2">Status</th>
              <th className="p-2">Ações</th>
            </tr>
          </thead>
          <tbody>
            {result.items.length === 0 && (
              <tr>
                <td colSpan={7} className="p-4 text-zinc-500">
                  Nenhum registro
                </td>
              </tr>
            )}
            {result.items.map((r) => (
              <tr key={r.pedidoItemId} className="border-t border-zinc-800">
                <td className="p-2 whitespace-nowrap">{r.dataPedido?.slice(0, 10)}</td>
                <td className="p-2">{r.pedidoExternoId}</td>
                <td className="p-2 max-w-[180px] truncate" title={r.nomeCliente}>
                  {r.nomeCliente} {r.clienteCpf}
                </td>
                <td className="p-2 max-w-[200px] truncate">
                  {r.produto} — {r.cor ?? "—"} — {r.tamanho ?? "—"}
                </td>
                <td className="p-2">{r.quantidade}</td>
                <td className="p-2">{r.impresso ? "Impresso" : "Pendente"}</td>
                <td className="p-2">
                  <PedidoRowActions
                    itemId={r.pedidoItemId}
                    accessToken={session.access_token}
                    quantidade={r.quantidade}
                  />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {result.totalPages > 1 && (
        <div className="mt-4 flex gap-2">
          {page > 1 && (
            <Link
              className="text-sm text-[#FFF200]"
              href={`/pedidos?${new URLSearchParams({ ...(q && { q }), ...(data && { data }), page: String(page - 1) }).toString()}`}
            >
              Anterior
            </Link>
          )}
          <span className="text-sm text-zinc-500">
            {page} / {result.totalPages} ({result.totalCount})
          </span>
          {page < result.totalPages && (
            <Link
              className="text-sm text-[#FFF200]"
              href={`/pedidos?${new URLSearchParams({ ...(q && { q }), ...(data && { data }), page: String(page + 1) }).toString()}`}
            >
              Próxima
            </Link>
          )}
        </div>
      )}
    </div>
  );
}
