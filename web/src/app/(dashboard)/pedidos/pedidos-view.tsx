"use client";

import Link from "next/link";
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
import { PedidoRowActions } from "./row-actions";
import { PrintAllPendingButton } from "./print-all-pending-button";
import { PaginationBar } from "@/components/PaginationBar";

type Row = {
  pedidoId: number;
  pedidoItemId: number;
  dataPedido: string;
  pedidoExternoId: string;
  nomeCliente: string;
  clienteCpf: string | null;
  produto: string;
  cor: string | null;
  tamanho: string | null;
  quantidade: number;
  impresso: boolean;
};

type Paged = {
  items: Row[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
};

export function PedidosView({
  result,
  q,
  data,
  page,
  accessToken,
}: {
  result: Paged;
  q?: string;
  data?: string;
  page: number;
  accessToken: string;
}) {
  const qs = (p: number) => {
    const sp = new URLSearchParams();
    if (q) sp.set("q", q);
    if (data) sp.set("data", data);
    sp.set("page", String(p));
    return sp.toString();
  };

  return (
    <Box>
      <Box
        sx={{
          display: "flex",
          flexDirection: { xs: "column", sm: "row" },
          alignItems: { xs: "flex-start", sm: "center" },
          justifyContent: "space-between",
          gap: 2,
          mb: 2,
        }}
      >
        <Typography variant="h5" sx={{ color: "primary.main" }}>
          Pedidos
        </Typography>
        <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1, alignItems: "center" }}>
          <Button component={Link} href="/pedidos/novo" variant="contained" color="secondary" size="small" sx={{ fontWeight: 800 }}>
            + Adicionar pedido
          </Button>
          <PrintAllPendingButton />
        </Box>
      </Box>
      <Paper
        variant="outlined"
        component="form"
        sx={{ p: 2, mb: 3, display: "flex", flexWrap: "wrap", gap: 2, alignItems: "flex-end" }}
      >
        <TextField name="q" label="Busca" size="small" defaultValue={q} placeholder="ID, nome, CPF" />
        <TextField name="data" label="Data" type="date" size="small" defaultValue={data} slotProps={{ inputLabel: { shrink: true } }} />
        <Button type="submit" variant="contained" color="primary">
          Filtrar
        </Button>
        <Button component={Link} href="/pedidos" variant="outlined" size="small">
          Limpar
        </Button>
      </Paper>
      <TableContainer component={Paper} variant="outlined">
        <Table size="small" sx={{ minWidth: 800 }}>
          <TableHead>
            <TableRow
              sx={{
                bgcolor: "grey.100",
                "& .MuiTableCell-head": { fontWeight: 700, color: "text.primary" },
              }}
            >
              <TableCell>Data</TableCell>
              <TableCell>Pedido</TableCell>
              <TableCell>Cliente</TableCell>
              <TableCell>Item</TableCell>
              <TableCell width={72} align="right">
                Qtd
              </TableCell>
              <TableCell>Status</TableCell>
              <TableCell width={200} align="right">
                Ações
              </TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {result.items.length === 0 && (
              <TableRow>
                <TableCell colSpan={7} sx={{ color: "text.secondary" }}>
                  Nenhum registro
                </TableCell>
              </TableRow>
            )}
            {result.items.map((r) => (
              <TableRow key={r.pedidoItemId} hover>
                <TableCell sx={{ whiteSpace: "nowrap" }}>{r.dataPedido?.slice(0, 10)}</TableCell>
                <TableCell>{r.pedidoExternoId}</TableCell>
                <TableCell sx={{ maxWidth: 200 }} title={r.nomeCliente}>
                  {r.nomeCliente} {r.clienteCpf}
                </TableCell>
                <TableCell sx={{ maxWidth: 220 }} title={`${r.produto} — ${r.cor} — ${r.tamanho}`}>
                  {r.produto} — {r.cor ?? "—"} — {r.tamanho ?? "—"}
                </TableCell>
                <TableCell align="right">{r.quantidade}</TableCell>
                <TableCell>
                  {r.impresso ? (
                    <Typography component="span" variant="body2" color="success.main" sx={{ fontWeight: 600 }}>
                      Impresso
                    </Typography>
                  ) : (
                    <Typography component="span" variant="body2" color="warning.dark">
                      Pendente
                    </Typography>
                  )}
                </TableCell>
                <TableCell align="right">
                  <PedidoRowActions itemId={r.pedidoItemId} accessToken={accessToken} quantidade={r.quantidade} />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
      <PaginationBar
        page={page}
        totalPages={result.totalPages}
        totalCount={result.totalCount}
        pageSize={result.pageSize}
        buildHref={(p) => `/pedidos?${qs(p)}`}
      />
    </Box>
  );
}
