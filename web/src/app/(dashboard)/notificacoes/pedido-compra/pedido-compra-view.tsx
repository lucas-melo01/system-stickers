"use client";

import { useState } from "react";
import Link from "next/link";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import Paper from "@mui/material/Paper";
import TextField from "@mui/material/TextField";
import MenuItem from "@mui/material/MenuItem";
import Button from "@mui/material/Button";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Chip from "@mui/material/Chip";
import Dialog from "@mui/material/Dialog";
import DialogTitle from "@mui/material/DialogTitle";
import DialogContent from "@mui/material/DialogContent";
import DialogActions from "@mui/material/DialogActions";
import { PaginationBar } from "@/components/PaginationBar";
import { formatDataHoraBR } from "@/lib/datetime";
import type { NotificacaoLista, Paged } from "@/lib/normalize-cadastros";

function statusColor(status: string): "success" | "error" | "warning" | "default" {
  if (status === "Enviado") return "success";
  if (status === "Falha") return "error";
  if (status === "Pendente") return "warning";
  return "default";
}

export function PedidoCompraView({
  initial,
  data,
  status,
  pedido,
  page,
}: {
  initial: Paged<NotificacaoLista>;
  data?: string;
  status?: string;
  pedido?: string;
  page: number;
}) {
  const [detalhe, setDetalhe] = useState<NotificacaoLista | null>(null);

  const buildHref = (p: number) => {
    const qs = new URLSearchParams();
    if (data) qs.set("data", data);
    if (status) qs.set("status", status);
    if (pedido) qs.set("pedido", pedido);
    qs.set("page", String(p));
    return `/notificacoes/pedido-compra?${qs.toString()}`;
  };

  return (
    <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
      <Typography variant="h5" color="primary" sx={{ fontWeight: 700 }}>
        Pedido de Compra
      </Typography>
      <Typography variant="body2" color="text.secondary">
        Histórico de notificações enviadas aos fornecedores para itens de encomenda (-enc).
      </Typography>

      <Paper variant="outlined" component="form" method="get" sx={{ p: 2 }}>
        <Box sx={{ display: "flex", flexWrap: "wrap", gap: 2, alignItems: "flex-end" }}>
          <TextField name="data" label="Data" type="date" defaultValue={data ?? ""} size="small" slotProps={{ inputLabel: { shrink: true } }} />
          <TextField name="status" select label="Status" defaultValue={status ?? ""} size="small" sx={{ minWidth: 140 }}>
            <MenuItem value="">Todos</MenuItem>
            <MenuItem value="Enviado">Enviado</MenuItem>
            <MenuItem value="Falha">Falha</MenuItem>
            <MenuItem value="Pendente">Pendente</MenuItem>
          </TextField>
          <TextField name="pedido" label="Pedido" defaultValue={pedido ?? ""} size="small" />
          <Button type="submit" variant="contained" color="primary">
            Filtrar
          </Button>
          {(data || status || pedido) && (
            <Button component={Link} href="/notificacoes/pedido-compra" variant="outlined">
              Limpar
            </Button>
          )}
        </Box>
      </Paper>

      <TableContainer component={Paper} variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow sx={{ bgcolor: "grey.100" }}>
              <TableCell>Data</TableCell>
              <TableCell>Pedido</TableCell>
              <TableCell>Cliente</TableCell>
              <TableCell>Fornecedor</TableCell>
              <TableCell>Produto</TableCell>
              <TableCell>Status</TableCell>
              <TableCell align="right">Detalhe</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {initial.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} align="center">
                  Nenhuma notificação encontrada.
                </TableCell>
              </TableRow>
            ) : (
              initial.items.map((n) => (
                <TableRow key={n.id} hover>
                  <TableCell>{formatDataHoraBR(n.criadoEm)}</TableCell>
                  <TableCell>{n.pedidoExternoId}</TableCell>
                  <TableCell>{n.nomeCliente}</TableCell>
                  <TableCell>{n.fornecedorNome ?? "—"}</TableCell>
                  <TableCell>{n.produtoNome ?? "—"}</TableCell>
                  <TableCell>
                    <Chip label={n.status} size="small" color={statusColor(n.status)} variant="outlined" />
                  </TableCell>
                  <TableCell align="right">
                    <Button size="small" onClick={() => setDetalhe(n)}>
                      Ver
                    </Button>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>

      <PaginationBar
        page={page}
        totalPages={initial.totalPages}
        totalCount={initial.totalCount}
        pageSize={initial.pageSize}
        buildHref={buildHref}
      />

      <Dialog open={!!detalhe} onClose={() => setDetalhe(null)} maxWidth="sm" fullWidth>
        <DialogTitle>Detalhe da notificação</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 1.5 }}>
          {detalhe && (
            <>
              <Typography variant="body2"><strong>Status:</strong> {detalhe.status}</Typography>
              <Typography variant="body2"><strong>Loja:</strong> {detalhe.loja}</Typography>
              <Typography variant="body2"><strong>Pedido:</strong> {detalhe.pedidoExternoId}</Typography>
              <Typography variant="body2"><strong>Cliente:</strong> {detalhe.nomeCliente}</Typography>
              <Typography variant="body2"><strong>Fornecedor:</strong> {detalhe.fornecedorNome ?? "—"}</Typography>
              <Typography variant="body2"><strong>Produto:</strong> {detalhe.produtoNome ?? "—"}</Typography>
              {detalhe.whatsAppMessageId && (
                <Typography variant="body2"><strong>ID WhatsApp:</strong> {detalhe.whatsAppMessageId}</Typography>
              )}
              {detalhe.erro && (
                <Typography variant="body2" color="error"><strong>Erro:</strong> {detalhe.erro}</Typography>
              )}
              <Typography variant="body2" sx={{ whiteSpace: "pre-wrap", mt: 1 }}>
                <strong>Mensagem:</strong>
                {"\n"}
                {detalhe.mensagemTexto}
              </Typography>
            </>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDetalhe(null)}>Fechar</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
