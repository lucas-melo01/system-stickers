"use client";

import { useEffect, useRef, useState } from "react";
import Button from "@mui/material/Button";
import Typography from "@mui/material/Typography";
import Box from "@mui/material/Box";
import { useRouter } from "next/navigation";

const PRINT_WINDOW_FEATURES = "width=560,height=520,resizable=yes,scrollbars=yes,noopener=no";

export function PrintAllPendingButton() {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);
  const popupRef = useRef<Window | null>(null);
  const watchTimer = useRef<number | null>(null);

  useEffect(() => {
    return () => {
      if (watchTimer.current) window.clearInterval(watchTimer.current);
    };
  }, []);

  function run() {
    if (
      !window.confirm(
        "Imprimir todas as etiquetas pendentes? Um único diálogo de impressão será aberto com todas elas."
      )
    ) {
      return;
    }
    setMsg(null);
    setLoading(true);
    const w = window.open("/print/etiquetas/pendentes", "print-pendentes", PRINT_WINDOW_FEATURES);
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
        <Typography variant="body2" color="text.secondary" sx={{ width: "100%" }}>
          {msg}
        </Typography>
      )}
    </Box>
  );
}
