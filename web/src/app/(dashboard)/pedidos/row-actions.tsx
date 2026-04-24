"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { createClient } from "@/lib/supabase/client";
import { isWebSerialSupported, printZplOverUsb } from "@/lib/print-zpl-serial";
import Button from "@mui/material/Button";
import FormControl from "@mui/material/FormControl";
import InputLabel from "@mui/material/InputLabel";
import MenuItem from "@mui/material/MenuItem";
import Select from "@mui/material/Select";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";

const BAUD_STORAGE = "zpl-serial-baud";
const BAUDS = [9600, 19200, 38400, 57600, 115200] as const;

export function PedidoRowActions({
  itemId,
  accessToken: tokenProp,
  quantidade,
}: {
  itemId: number;
  accessToken: string;
  quantidade: number;
}) {
  const router = useRouter();
  const [msg, setMsg] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [baud, setBaud] = useState(() => {
    if (typeof window === "undefined") return 9600;
    const s = window.localStorage.getItem(BAUD_STORAGE);
    const n = s ? parseInt(s, 10) : 9600;
    return (BAUDS as readonly number[]).includes(n) ? n : 9600;
  });

  function persistBaud(n: number) {
    setBaud(n);
    try {
      window.localStorage.setItem(BAUD_STORAGE, String(n));
    } catch {
      /* ignore */
    }
  }

  async function getToken() {
    const supabase = createClient();
    const {
      data: { session },
    } = await supabase.auth.getSession();
    return session?.access_token ?? tokenProp;
  }

  async function marcar(t: string) {
    const r = await fetch(`/api/pedido-itens/${itemId}/marcar-impresso`, {
      method: "POST",
      headers: { Authorization: `Bearer ${t}` },
    });
    if (!r.ok) throw new Error(await r.text());
  }

  async function imprimir() {
    setMsg(null);
    setLoading(true);
    const copies = Math.max(1, Math.min(quantidade || 1, 50));
    try {
      if (!isWebSerialSupported()) {
        setMsg(
          "Navegador sem Web Serial. Use Chrome ou Edge (HTTPS) e a porta USB (COM) da impressora."
        );
        return;
      }
      const t = await getToken();
      if (!t) {
        setMsg("Sessão expirou. Entre de novo.");
        return;
      }
      const r = await fetch(`/api/pedido-itens/${itemId}/zpl-json`, {
        headers: { Authorization: `Bearer ${t}` },
      });
      if (!r.ok) throw new Error((await r.text()) || "Falha ao obter ZPL");
      const j = (await r.json()) as { zpl: string };
      if (!j.zpl?.length) throw new Error("ZPL vazio");

      for (let i = 0; i < copies; i++) {
        await printZplOverUsb(j.zpl, baud);
      }

      await marcar(t);
      setMsg("Impresso e atualizado");
      router.refresh();
    } catch (e) {
      setMsg(String(e));
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
      <FormControl size="small" sx={{ minWidth: 88 }}>
        <InputLabel id={`baud-${itemId}`} sx={{ fontSize: 12 }}>
          Baud
        </InputLabel>
        <Select
          labelId={`baud-${itemId}`}
          value={baud}
          label="Baud"
          onChange={(e) => persistBaud(Number(e.target.value))}
          title="Tente 115200 se 9600 imprimir lixo"
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
        disabled={loading}
        onClick={imprimir}
        variant="contained"
        size="small"
        color="primary"
        sx={{ minWidth: 90, fontWeight: 700, whiteSpace: "nowrap" }}
      >
        {loading ? "…" : "Imprimir"}
      </Button>
      {msg && (
        <Typography variant="caption" color="text.secondary" sx={{ width: "100%", textAlign: "right" }}>
          {msg}
        </Typography>
      )}
    </Box>
  );
}
