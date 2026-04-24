"use client";

import { useState, useEffect } from "react";
import { createClient } from "@/lib/supabase/client";
import { printItemLabelsBatched } from "@/lib/etiqueta-print";
import Button from "@mui/material/Button";
import FormControl from "@mui/material/FormControl";
import InputLabel from "@mui/material/InputLabel";
import MenuItem from "@mui/material/MenuItem";
import Select from "@mui/material/Select";
import Typography from "@mui/material/Typography";
import Box from "@mui/material/Box";
import { useRouter } from "next/navigation";

const BAUD_STORAGE = "zpl-serial-baud";
const BAUDS = [9600, 19200, 38400, 57600, 115200] as const;

type Pendente = { itemId: number; quantidade: number };

export function PrintAllPendingButton() {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);
  const [baud, setBaud] = useState(9600);

  useEffect(() => {
    const s = window.localStorage.getItem(BAUD_STORAGE);
    const n = s ? parseInt(s, 10) : 9600;
    if ((BAUDS as readonly number[]).includes(n)) setBaud(n);
  }, []);

  function persistBaud(n: number) {
    setBaud(n);
    try {
      window.localStorage.setItem(BAUD_STORAGE, String(n));
    } catch {
      /* ignore */
    }
  }

  async function run() {
    if (!window.confirm("Imprimir todas as etiquetas pendentes, na ordem da base? A impressora USB deve estar pronta.")) {
      return;
    }
    setMsg(null);
    setLoading(true);
    try {
      const supabase = createClient();
      const {
        data: { session },
      } = await supabase.auth.getSession();
      const t = session?.access_token;
      if (!t) {
        setMsg("Sessão expirou.");
        return;
      }
      const r = await fetch("/api/pedido-itens/pendentes-impressao", {
        headers: { Authorization: `Bearer ${t}` },
      });
      if (!r.ok) throw new Error(await r.text());
      const list = (await r.json()) as Pendente[];
      if (list.length === 0) {
        setMsg("Não há itens pendentes.");
        return;
      }
      for (const row of list) {
        const q = row.quantidade > 0 ? row.quantidade : 1;
        await printItemLabelsBatched(row.itemId, t, baud, q);
      }
      setMsg(`Concluído: ${list.length} item(ns) processado(s).`);
      router.refresh();
    } catch (e) {
      setMsg(String(e));
    } finally {
      setLoading(false);
    }
  }

  return (
    <Box sx={{ display: "inline-flex", flexWrap: "wrap", alignItems: "center", gap: 1 }}>
      <FormControl size="small" sx={{ minWidth: 100 }}>
        <InputLabel id="baud-lote">Baud</InputLabel>
        <Select
          labelId="baud-lote"
          label="Baud"
          value={baud}
          onChange={(e) => persistBaud(Number(e.target.value))}
        >
          {BAUDS.map((b) => (
            <MenuItem key={b} value={b}>
              {b}
            </MenuItem>
          ))}
        </Select>
      </FormControl>
      <Button
        type="button"
        variant="contained"
        color="primary"
        disabled={loading}
        onClick={run}
        sx={{ fontWeight: 700 }}
      >
        {loading ? "A processar…" : "Imprimir todas as pendentes"}
      </Button>
      {msg && (
        <Typography variant="body2" color="text.secondary" sx={{ width: "100%" }}>
          {msg}
        </Typography>
      )}
    </Box>
  );
}
