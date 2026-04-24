"use client";

import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import Button from "@mui/material/Button";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";

const PRINT_WINDOW_FEATURES = "width=520,height=420,resizable=yes,scrollbars=yes,noopener=no";

export function PedidoRowActions({ itemId }: { itemId: number }) {
  const router = useRouter();
  const [msg, setMsg] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  // Detecta o fecho da janela de impressão para poder recarregar a tabela,
  // mostrando o item como "impresso" sem o utilizador precisar de refresh manual.
  const popupRef = useRef<Window | null>(null);
  const watchTimer = useRef<number | null>(null);

  useEffect(() => {
    return () => {
      if (watchTimer.current) window.clearInterval(watchTimer.current);
    };
  }, []);

  function imprimir() {
    setMsg(null);
    setLoading(true);
    const w = window.open(`/print/etiqueta/${itemId}`, `print-etiqueta-${itemId}`, PRINT_WINDOW_FEATURES);
    if (!w) {
      setLoading(false);
      setMsg("O browser bloqueou o pop-up. Autorize pop-ups para este site.");
      return;
    }
    popupRef.current = w;
    if (watchTimer.current) window.clearInterval(watchTimer.current);
    watchTimer.current = window.setInterval(() => {
      if (!popupRef.current || popupRef.current.closed) {
        if (watchTimer.current) window.clearInterval(watchTimer.current);
        watchTimer.current = null;
        popupRef.current = null;
        setLoading(false);
        router.refresh();
      }
    }, 600);
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
        <Typography variant="caption" color="text.secondary" sx={{ width: "100%", textAlign: "right" }}>
          {msg}
        </Typography>
      )}
    </Box>
  );
}
