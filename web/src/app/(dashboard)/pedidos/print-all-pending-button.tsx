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
  // IDs de PedidoItem selecionados pelo operador. Quando preenchido,
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

  // Botão da selecção só faz sentido quando há itens selecionados.
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
            ? "Nenhum item encontrado entre os selecionados."
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

      const itemIds = lista.map((x) => x.itemId);
      const mr = await fetch(`/api/pedido-itens/marcar-impresso-lote`, {
        method: "POST",
        headers: {
          Authorization: `Bearer ${token}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ itemIds }),
      });
      if (!mr.ok) {
        throw new Error(
          (await mr.text()) ||
            `Falha ao marcar etiquetas como impressas (HTTP ${mr.status}). Os rótulos podem já ter sido enviados à impressora.`
        );
      }

      let marcados = itemIds.length;
      let missing: number[] = [];
      try {
        const parsed = (await mr.json()) as {
          marcados?: number;
          missingItemIds?: number[];
        };
        if (typeof parsed.marcados === "number") marcados = parsed.marcados;
        if (Array.isArray(parsed.missingItemIds)) missing = parsed.missingItemIds;
      } catch {
        // resposta já consumida só se texto vazio — mantém marcados=lista.length
      }

      if (missing.length > 0 || marcados !== lista.length) {
        const detalhe =
          missing.length > 0
            ? `Itens não encontrados na base: ${missing.join(", ")}.`
            : `Contagem marcada (${marcados}) menor que etiquetas imprimidas (${lista.length}); pode haver duplicados na resposta à impressora.`;
        setErro(
          `${marcados} de ${lista.length} etiqueta(s) marcada(s) no sistema. ${detalhe} As físicas já podem ter sido impressas — verifique a lista ou tente novamente.`
        );
        setMsg(`${lista.length} etiqueta(s) enviada(s) para ${printer}.`);
      } else {
        setMsg(`${lista.length} etiqueta(s) enviada(s) para ${printer} e marcada(s) no sistema.`);
      }
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
      const base = `Imprimir selecionados (${ids.length})`;
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
