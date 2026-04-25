"use client";

import { useState } from "react";
import Button from "@mui/material/Button";
import Typography from "@mui/material/Typography";
import Box from "@mui/material/Box";
import { useRouter } from "next/navigation";
import { createClient } from "@/lib/supabase/client";
import { printZplBatch, resolvePrinter, QzNotAvailableError } from "@/lib/qz-print";

type ZplItem = { itemId: number; zpl: string };

export function PrintAllPendingButton() {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);
  const [erro, setErro] = useState<string | null>(null);

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

      // 1. Pede o lote de ZPLs ao backend.
      const r = await fetch("/api/pedido-itens/pendentes-impressao/zpl", {
        headers: { Authorization: `Bearer ${token}` },
        cache: "no-store",
      });
      if (!r.ok) throw new Error((await r.text()) || `HTTP ${r.status}`);
      const lista = (await r.json()) as ZplItem[];
      if (!Array.isArray(lista) || lista.length === 0) {
        setMsg("Não há etiquetas pendentes.");
        return;
      }

      if (
        !window.confirm(
          `Imprimir ${lista.length} etiqueta(s) pendente(s)? Será enviado directo à impressora.`
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

  return (
    <Box sx={{ display: "inline-flex", flexWrap: "wrap", alignItems: "center", gap: 1 }}>
      <Button
        type="button"
        variant="contained"
        color="primary"
        disabled={loading}
        onClick={run}
        sx={{ fontWeight: 700 }}
      >
        {loading ? "A imprimir…" : "Imprimir todas as pendentes"}
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
