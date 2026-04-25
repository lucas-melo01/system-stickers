"use client";

import { useState } from "react";
import Button from "@mui/material/Button";
import Typography from "@mui/material/Typography";
import Box from "@mui/material/Box";
import { useRouter } from "next/navigation";
import { createClient } from "@/lib/supabase/client";
import { printZplBatch, resolvePrinter, QzNotAvailableError } from "@/lib/qz-print";

type ZplItem = { itemId: number; zpl: string };

type Props = {
  // Filtros activos na página — usados quando não há selecção explícita.
  q?: string;
  data?: string;
  // IDs de PedidoItem seleccionados pelo operador. Quando preenchido,
  // tem precedência absoluta sobre os filtros (q/data são ignorados).
  selectedIds?: number[];
  // Permite ao componente pai limpar a selecção depois da impressão.
  onPrinted?: () => void;
  // Variante visual: o botão da selecção destaca-se em "secondary".
  variant?: "todos" | "selecionados";
};

export function PrintAllPendingButton({
  q,
  data,
  selectedIds,
  onPrinted,
  variant = "todos",
}: Props) {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);
  const [erro, setErro] = useState<string | null>(null);

  const isSelecao = variant === "selecionados";
  const ids = selectedIds ?? [];
  const temSelecao = ids.length > 0;
  const temFiltro = Boolean(q || data);

  // Botão da selecção só faz sentido quando há itens seleccionados.
  if (isSelecao && !temSelecao) return null;

  async function run() {
    setMsg(null);
    setErro(null);
    setLoading(true);
    try {
      const supabase = createClient();
      const {
        data: { session },
      } = await supabase.auth.getSession();
      const token = session?.access_token;
      if (!token) throw new Error("Sessão expirou. Volte a entrar.");

      const sp = new URLSearchParams();
      if (isSelecao) {
        sp.set("ids", ids.join(","));
      } else {
        if (q) sp.set("q", q);
        if (data) sp.set("data", data);
      }
      const qsStr = sp.toString();
      const url = `/api/pedido-itens/pendentes-impressao/zpl${qsStr ? `?${qsStr}` : ""}`;

      // 1. Pede o lote de ZPLs ao backend (já filtrado).
      const r = await fetch(url, {
        headers: { Authorization: `Bearer ${token}` },
        cache: "no-store",
      });
      if (!r.ok) throw new Error((await r.text()) || `HTTP ${r.status}`);
      const lista = (await r.json()) as ZplItem[];
      if (!Array.isArray(lista) || lista.length === 0) {
        setMsg(
          isSelecao
            ? "Nenhum item pendente entre os seleccionados."
            : temFiltro
              ? "Não há etiquetas pendentes para o filtro actual."
              : "Não há etiquetas pendentes."
        );
        return;
      }

      const descritivo = isSelecao
        ? "seleccionada(s)"
        : temFiltro
          ? "pendente(s) do filtro actual"
          : "pendente(s)";

      if (
        !window.confirm(
          `Imprimir ${lista.length} etiqueta(s) ${descritivo}? Será enviado directo à impressora.`
        )
      ) {
        return;
      }

      // 2. Resolve impressora e envia tudo numa só operação.
      const printer = await resolvePrinter();
      await printZplBatch(printer, lista.map((x) => x.zpl));

      // 3. Marca todas como impressas em paralelo.
      await Promise.allSettled(
        lista.map((it) =>
          fetch(`/api/pedido-itens/${it.itemId}/marcar-impresso`, {
            method: "POST",
            headers: { Authorization: `Bearer ${token}` },
          })
        )
      );

      setMsg(`${lista.length} etiqueta(s) enviada(s) para ${printer}.`);
      onPrinted?.();
      router.refresh();
    } catch (e) {
      if (e instanceof QzNotAvailableError) {
        setErro(
          "QZ Tray não detectado. Instale o programa de impressão (ver banner no topo)."
        );
      } else {
        setErro(String(e instanceof Error ? e.message : e));
      }
    } finally {
      setLoading(false);
    }
  }

  const label = (() => {
    if (isSelecao) {
      const base = `Imprimir seleccionados (${ids.length})`;
      return loading ? "A imprimir…" : base;
    }
    if (loading) return "A imprimir…";
    return temFiltro ? "Imprimir pendentes do filtro" : "Imprimir todas as pendentes";
  })();

  return (
    <Box sx={{ display: "inline-flex", flexWrap: "wrap", alignItems: "center", gap: 1 }}>
      <Button
        type="button"
        variant="contained"
        color={isSelecao ? "secondary" : "primary"}
        disabled={loading}
        onClick={run}
        sx={{ fontWeight: 700 }}
      >
        {label}
      </Button>
      {msg && (
        <Typography variant="body2" color="success.main" sx={{ width: "100%" }}>
          {msg}
        </Typography>
      )}
      {erro && (
        <Typography variant="body2" color="error.main" sx={{ width: "100%" }}>
          {erro}
        </Typography>
      )}
    </Box>
  );
}
