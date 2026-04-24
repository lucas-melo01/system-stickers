"use client";

import { useState } from "react";
import { createClient } from "@/lib/supabase/client";
import { apiUrl } from "@/lib/api";

export function ExportExcelButton({ inicio, fim }: { inicio: string; fim: string }) {
  const [loading, setLoading] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  async function download() {
    setErr(null);
    setLoading(true);
    try {
      const supabase = createClient();
      const { data: { session } } = await supabase.auth.getSession();
      if (!session?.access_token) throw new Error("Sessão expirou");
      const u = new URLSearchParams({ inicio, fim });
      const r = await fetch(apiUrl(`/api/relatorios/vendas/export.xlsx?${u}`), {
        headers: { Authorization: `Bearer ${session.access_token}` },
      });
      if (!r.ok) throw new Error(await r.text());
      const blob = await r.blob();
      const a = document.createElement("a");
      a.href = URL.createObjectURL(blob);
      a.download = `Relatorio_Vendas_${inicio}_a_${fim}.xlsx`;
      a.click();
      URL.revokeObjectURL(a.href);
    } catch (e) {
      setErr(String(e));
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="mb-4">
      <button
        type="button"
        disabled={loading}
        onClick={download}
        className="px-3 py-1.5 text-sm text-[#001623] bg-[#FFF200] font-semibold rounded disabled:opacity-50"
      >
        {loading ? "Gerando…" : "Baixar Excel"}
      </button>
      {err && <p className="text-red-400 text-sm mt-1">{err}</p>}
    </div>
  );
}
