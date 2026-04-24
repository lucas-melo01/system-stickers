"use client";

import Button from "@mui/material/Button";
import Paper from "@mui/material/Paper";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import Box from "@mui/material/Box";
import { ExportExcelButton } from "./export-button";

type Venda = {
  dataPedido: string;
  sku: string;
  vendedor: string;
  peca: string;
  cliente: string;
  tipoEnvio: string;
  valorCusto: number;
  valorVenda: number;
  formaPagamento: string;
  valorFrete: number;
};

export function RelatoriosView({
  inicio,
  fim,
  rows,
  err,
}: {
  inicio?: string;
  fim?: string;
  rows: Venda[];
  err: string | null;
}) {
  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 2, color: "primary.main" }}>
        Relatórios de vendas
      </Typography>
      <Paper variant="outlined" component="form" sx={{ p: 2, mb: 3, display: "flex", flexWrap: "wrap", gap: 2, alignItems: "flex-end" }}>
        <TextField
          name="inicio"
          label="Início"
          type="date"
          size="small"
          defaultValue={inicio}
          required
          slotProps={{ inputLabel: { shrink: true } }}
        />
        <TextField
          name="fim"
          label="Fim"
          type="date"
          size="small"
          defaultValue={fim}
          required
          slotProps={{ inputLabel: { shrink: true } }}
        />
        <Button type="submit" variant="contained" color="primary">
          Aplicar
        </Button>
      </Paper>
      {err && (
        <Typography color="error" sx={{ mb: 2 }}>
          {err}
        </Typography>
      )}
      {inicio && fim && rows.length > 0 && <ExportExcelButton inicio={inicio} fim={fim} />}

      <TableContainer component={Paper} variant="outlined" sx={{ mt: inicio && fim && rows.length > 0 ? 2 : 0 }}>
        <Table size="small">
          <TableHead>
            <TableRow sx={{ bgcolor: "grey.100" }}>
              <TableCell>Data</TableCell>
              <TableCell>SKU</TableCell>
              <TableCell>Vendedor</TableCell>
              <TableCell>Peça</TableCell>
              <TableCell>Cliente</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {rows.map((r, i) => (
              <TableRow key={i} hover>
                <TableCell sx={{ whiteSpace: "nowrap" }}>{r.dataPedido?.slice(0, 10)}</TableCell>
                <TableCell>{r.sku}</TableCell>
                <TableCell>{r.vendedor}</TableCell>
                <TableCell sx={{ maxWidth: 240 }} title={r.peca}>
                  {r.peca}
                </TableCell>
                <TableCell>{r.cliente}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  );
}
