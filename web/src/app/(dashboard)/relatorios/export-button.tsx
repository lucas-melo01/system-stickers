"use client";

import { useState } from "react";
import { createClient } from "@/lib/supabase/client";
import Button from "@mui/material/Button";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";

export function ExportExcelButton({ inicio, fim }: { inicio: string; fim: string }) {
  const [loading, setLoading] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  async function download() {
    setErr(null);
    setLoading(true);
    try {
      const supabase = createClient();
      const {
        data: { session },
      } = await supabase.auth.getSession();
      if (!session?.access_token) throw new Error("Sessão expirou");
      const u = new URLSearchParams({ inicio, fim });
      const r = await fetch(`/api/relatorios/vendas/export?${u}`, {
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
    <Box>
      <Button
        type="button"
        disabled={loading}
        onClick={download}
        variant="contained"
        color="secondary"
        size="small"
        sx={{ fontWeight: 700 }}
      >
        {loading ? "Gerando…" : "Baixar Excel"}
      </Button>
      {err && (
        <Typography color="error" variant="body2" sx={{ mt: 1 }}>
          {err}
        </Typography>
      )}
    </Box>
  );
}
