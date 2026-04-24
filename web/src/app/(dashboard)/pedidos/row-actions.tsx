"use client";

import { useState } from "react";
import { apiPost } from "@/lib/api";
import { createClient } from "@/lib/supabase/client";

export function PedidoRowActions({
  itemId,
  printUrl,
  accessToken: tokenProp,
}: {
  itemId: number;
  printUrl: string;
  accessToken: string;
}) {
  const [msg, setMsg] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function getToken() {
    const supabase = createClient();
    const { data: { session } } = await supabase.auth.getSession();
    return session?.access_token ?? tokenProp;
  }

  async function copiarZpl() {
    setMsg(null);
    setLoading(true);
    try {
      const t = await getToken();
      if (!t) {
        setMsg("Sem sessão");
        return;
      }
      const r = await fetch(printUrl, { headers: { Authorization: `Bearer ${t}` } });
      if (!r.ok) throw new Error(await r.text());
      const j = (await r.json()) as { zpl: string };
      await navigator.clipboard.writeText(j.zpl);
      setMsg("ZPL copiado");
    } catch (e) {
      setMsg(String(e));
    } finally {
      setLoading(false);
    }
  }

  async function marcarImpresso() {
    if (!confirm("Marcar item como impresso?")) return;
    setMsg(null);
    setLoading(true);
    try {
      const t = await getToken();
      if (!t) {
        setMsg("Sem sessão");
        return;
      }
      await apiPost(`/api/pedido-itens/${itemId}/marcar-impresso`, t);
      setMsg("Atualizado");
      window.location.reload();
    } catch (e) {
      setMsg(String(e));
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="flex flex-col gap-1">
      <div className="flex flex-wrap gap-1">
        <button
          type="button"
          disabled={loading}
          onClick={copiarZpl}
          className="text-xs px-2 py-0.5 rounded bg-zinc-800 border border-zinc-600"
        >
          Copiar ZPL
        </button>
        <button
          type="button"
          disabled={loading}
          onClick={marcarImpresso}
          className="text-xs px-2 py-0.5 rounded bg-zinc-800 border border-zinc-600"
        >
          Marcar impresso
        </button>
      </div>
      {msg && <span className="text-xs text-zinc-500">{msg}</span>}
    </div>
  );
}
