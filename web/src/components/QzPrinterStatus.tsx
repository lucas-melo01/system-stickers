"use client";

import { useEffect, useState } from "react";
import Alert from "@mui/material/Alert";
import AlertTitle from "@mui/material/AlertTitle";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import FormControl from "@mui/material/FormControl";
import InputLabel from "@mui/material/InputLabel";
import MenuItem from "@mui/material/MenuItem";
import Select from "@mui/material/Select";
import Typography from "@mui/material/Typography";
import {
  ensureQzConnected,
  getDefaultPrinter,
  getPreferredPrinter,
  listPrinters,
  setPreferredPrinter,
} from "@/lib/qz-print";

const QZ_DOWNLOAD_URL = "https://qz.io/download/";

type Status = "loading" | "ok" | "missing";

export function QzPrinterStatus() {
  const [status, setStatus] = useState<Status>("loading");
  const [printer, setPrinter] = useState<string | null>(null);
  const [open, setOpen] = useState(false);
  const [printers, setPrinters] = useState<string[]>([]);
  const [selected, setSelected] = useState<string>("");
  const [erroDialogo, setErroDialogo] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        await ensureQzConnected();
        if (cancelled) return;
        const pref = getPreferredPrinter();
        const def = await getDefaultPrinter().catch(() => "");
        setPrinter(pref || def || null);
        setStatus("ok");
      } catch {
        if (cancelled) return;
        setStatus("missing");
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  async function abrirDialogo() {
    setErroDialogo(null);
    try {
      const lista = await listPrinters();
      setPrinters(lista);
      setSelected(getPreferredPrinter() ?? printer ?? "");
      setOpen(true);
    } catch (e) {
      setErroDialogo(String(e instanceof Error ? e.message : e));
      setOpen(true);
    }
  }

  function gravar() {
    setPreferredPrinter(selected || null);
    setPrinter(selected || null);
    setOpen(false);
  }

  if (status === "loading") {
    return null;
  }

  if (status === "missing") {
    return (
      <Alert
        severity="warning"
        sx={{ mb: 2 }}
        action={
          <Button
            color="warning"
            variant="contained"
            size="small"
            href={QZ_DOWNLOAD_URL}
            target="_blank"
            rel="noopener noreferrer"
          >
            Descarregar QZ Tray
          </Button>
        }
      >
        <AlertTitle sx={{ fontWeight: 700 }}>Programa de impressão não detectado</AlertTitle>
        Para imprimir directamente para a sua impressora térmica é necessário ter o
        <strong> QZ Tray </strong> instalado e em execução nesta máquina. Após instalar, recarregue a página.
      </Alert>
    );
  }

  return (
    <Box sx={{ mb: 2, display: "flex", alignItems: "center", gap: 1, flexWrap: "wrap" }}>
      <Chip
        size="small"
        color="success"
        variant="outlined"
        label="Impressão pronta"
        sx={{ fontWeight: 700 }}
      />
      <Typography variant="body2" color="text.secondary">
        Impressora actual: <strong>{printer || "(padrão do Windows)"}</strong>
      </Typography>
      <Button size="small" variant="text" onClick={abrirDialogo}>
        Mudar impressora
      </Button>

      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>Escolher impressora</DialogTitle>
        <DialogContent>
          {erroDialogo ? (
            <Typography color="error" variant="body2">
              {erroDialogo}
            </Typography>
          ) : (
            <FormControl fullWidth sx={{ mt: 1 }}>
              <InputLabel id="qz-printer-label">Impressora</InputLabel>
              <Select
                labelId="qz-printer-label"
                label="Impressora"
                value={selected}
                onChange={(e) => setSelected(String(e.target.value))}
              >
                <MenuItem value="">
                  <em>(usar padrão do Windows)</em>
                </MenuItem>
                {printers.map((p) => (
                  <MenuItem key={p} value={p}>
                    {p}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          )}
          <Typography variant="caption" color="text.secondary" sx={{ display: "block", mt: 2 }}>
            A escolha fica guardada apenas neste browser. Para outras máquinas, escolha de novo.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>Cancelar</Button>
          <Button onClick={gravar} variant="contained" disabled={!!erroDialogo}>
            Gravar
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
