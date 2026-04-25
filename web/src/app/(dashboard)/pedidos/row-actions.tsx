"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Button from "@mui/material/Button";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import { createClient } from "@/lib/supabase/client";
import { printZpl, resolvePrinter, QzNotAvailableError } from "@/lib/qz-print";

type ZplResponse = { zpl: string };

export function PedidoRowActions({ itemId }: { itemId: number }) {
  const router = useRouter();
  const [msg, setMsg] = useState<string | null>(null);
  const [erro, setErro] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function imprimir() {
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

      // 1. Pede o ZPL ao backend.
      const r = await fetch(`/api/pedido-itens/${itemId}/zpl-json`, {
        headers: { Authorization: `Bearer ${token}` },
        cache: "no-store",
      });
      if (!r.ok) throw new Error((await r.text()) || `HTTP ${r.status}`);
      const j = (await r.json()) as ZplResponse;
      if (!j.zpl) throw new Error("Resposta sem ZPL.");

      // 2. Resolve impressora preferida do operador (ou padrão do Windows).
      const printer = await resolvePrinter();

      // 3. Envia para o QZ Tray local — sem diálogos, sem driver a interferir.
      await printZpl(printer, j.zpl);

      // 4. Marca como impresso.
      await fetch(`/api/pedido-itens/${itemId}/marcar-impresso`, {
        method: "POST",
        headers: { Authorization: `Bearer ${token}` },
      });

      setMsg(`Enviado para ${printer}.`);
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
    <Box
      sx={{
        display: "flex",
        flexDirection: "row",
        flexWrap: "wrap",
        gap: 0.5,
        alignItems: "center",
        justifyContent: "flex-end",
      }}
    >
      <Button
        type="button"
        disabled={loading}
        onClick={imprimir}
        variant="contained"
        size="small"
        color="primary"
        sx={{ minWidth: 110, fontWeight: 700, whiteSpace: "nowrap" }}
      >
        {loading ? "A imprimir…" : "Imprimir"}
      </Button>
      {msg && (
        <Typography variant="caption" color="success.main" sx={{ width: "100%", textAlign: "right" }}>
          {msg}
        </Typography>
      )}
      {erro && (
        <Typography variant="caption" color="error.main" sx={{ width: "100%", textAlign: "right" }}>
          {erro}
        </Typography>
      )}
    </Box>
  );
}
