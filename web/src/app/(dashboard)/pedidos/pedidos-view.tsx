"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import Button from "@mui/material/Button";
import Checkbox from "@mui/material/Checkbox";
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
import { QzPrinterStatus } from "@/components/QzPrinterStatus";
import { formatDataHoraBR } from "@/lib/datetime";

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
}: {
  result: Paged;
  q?: string;
  data?: string;
  page: number;
}) {
  const qs = (p: number) => {
    const sp = new URLSearchParams();
    if (q) sp.set("q", q);
    if (data) sp.set("data", data);
    sp.set("page", String(p));
    return sp.toString();
  };

  // IDs da página actual — seleccionáveis para impressão em massa (inclui reimpressão).
  const idsNaPagina = useMemo(
    () => result.items.map((r) => r.pedidoItemId),
    [result.items]
  );

  const [selecionados, setSelecionados] = useState<Set<number>>(new Set());

  // Ao mudar de página/filtro a selecção visível seria enganadora — limpamos.
  // Identificamos a "página" pelo conjunto de IDs presentes na resposta.
  const idsResposta = useMemo(
    () => result.items.map((r) => r.pedidoItemId).join(","),
    [result.items]
  );
  useEffect(() => {
    setSelecionados(new Set());
  }, [idsResposta]);

  const todosSelecionados =
    idsNaPagina.length > 0 && idsNaPagina.every((id) => selecionados.has(id));
  const algumSelecionado =
    idsNaPagina.some((id) => selecionados.has(id)) && !todosSelecionados;

  function toggleTodos() {
    setSelecionados((prev) => {
      const next = new Set(prev);
      if (todosSelecionados) {
        for (const id of idsNaPagina) next.delete(id);
      } else {
        for (const id of idsNaPagina) next.add(id);
      }
      return next;
    });
  }

  function toggleUm(id: number) {
    setSelecionados((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  const idsSelecionados = useMemo(() => Array.from(selecionados), [selecionados]);

  return (
    <Box>
      <QzPrinterStatus />
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
          <PrintAllPendingButton
            variant="selecionados"
            selectedIds={idsSelecionados}
            onPrinted={() => setSelecionados(new Set())}
          />
          <PrintAllPendingButton q={q} data={data} />
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
              <TableCell padding="checkbox">
                <Checkbox
                  size="small"
                  color="primary"
                  disabled={idsNaPagina.length === 0}
                  checked={todosSelecionados}
                  indeterminate={algumSelecionado}
                  onChange={toggleTodos}
                  slotProps={{ input: { "aria-label": "Seleccionar todos os itens da página" } }}
                />
              </TableCell>
              <TableCell>Data</TableCell>
              <TableCell>Pedido</TableCell>
              <TableCell>Cliente</TableCell>
              <TableCell>Item</TableCell>
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
            {result.items.map((r) => {
              const checked = selecionados.has(r.pedidoItemId);
              return (
                <TableRow
                  key={r.pedidoItemId}
                  hover
                  selected={checked}
                  onClick={(e) => {
                    const target = e.target as HTMLElement;
                    if (target.closest("button, a, input")) return;
                    toggleUm(r.pedidoItemId);
                  }}
                  sx={{ cursor: "pointer" }}
                >
                  <TableCell padding="checkbox">
                    <Checkbox
                      size="small"
                      color="primary"
                      checked={checked}
                      onChange={() => toggleUm(r.pedidoItemId)}
                      slotProps={{ input: { "aria-label": `Seleccionar item ${r.pedidoItemId}` } }}
                    />
                  </TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{formatDataHoraBR(r.dataPedido)}</TableCell>
                  <TableCell>{r.pedidoExternoId}</TableCell>
                  <TableCell sx={{ maxWidth: 200 }} title={r.nomeCliente}>
                    {r.nomeCliente} {r.clienteCpf}
                  </TableCell>
                  <TableCell sx={{ maxWidth: 220 }} title={`${r.produto} — ${r.cor} — ${r.tamanho}`}>
                    {r.produto} — {r.cor ?? "—"} — {r.tamanho ?? "—"}
                  </TableCell>
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
                    <PedidoRowActions itemId={r.pedidoItemId} />
                  </TableCell>
                </TableRow>
              );
            })}
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
