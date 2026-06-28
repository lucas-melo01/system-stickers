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
import { PaginationBar } from "@/components/PaginationBar";
import type { Paged, ProdutoLista } from "@/lib/normalize-cadastros";

export function ProdutosView({
  initial,
  q,
  loja,
  semFornecedor,
  page,
}: {
  initial: Paged<ProdutoLista>;
  q?: string;
  loja?: string;
  semFornecedor?: boolean;
  page: number;
}) {
  const buildHref = (p: number) => {
    const qs = new URLSearchParams();
    if (q) qs.set("q", q);
    if (loja) qs.set("loja", loja);
    if (semFornecedor) qs.set("semFornecedor", "1");
    qs.set("page", String(p));
    return `/cadastros/produtos?${qs.toString()}`;
  };

  return (
    <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
      <Typography variant="h5" color="primary" sx={{ fontWeight: 700 }}>
        Produtos
      </Typography>
      <Typography variant="body2" color="text.secondary">
        Catálogo sincronizado com a Loja Integrada. O vínculo com fornecedor é feito na tela de Fornecedores.
      </Typography>

      <Paper variant="outlined" component="form" method="get" sx={{ p: 2 }}>
        <Box sx={{ display: "flex", flexWrap: "wrap", gap: 2, alignItems: "flex-end" }}>
          <TextField name="q" label="Buscar" defaultValue={q ?? ""} size="small" sx={{ minWidth: 200 }} />
          <TextField name="loja" select label="Loja" defaultValue={loja ?? ""} size="small" sx={{ minWidth: 160 }}>
            <MenuItem value="">Todas</MenuItem>
            <MenuItem value="Resume Modas">Resume Modas</MenuItem>
            <MenuItem value="DonnaKora">DonnaKora</MenuItem>
          </TextField>
          <TextField
            name="semFornecedor"
            select
            label="Fornecedor"
            defaultValue={semFornecedor ? "1" : ""}
            size="small"
            sx={{ minWidth: 180 }}
          >
            <MenuItem value="">Todos</MenuItem>
            <MenuItem value="1">Sem fornecedor</MenuItem>
          </TextField>
          <Button type="submit" variant="contained" color="primary">
            Filtrar
          </Button>
          {(q || loja || semFornecedor) && (
            <Button component={Link} href="/cadastros/produtos" variant="outlined">
              Limpar
            </Button>
          )}
        </Box>
      </Paper>

      <TableContainer component={Paper} variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow sx={{ bgcolor: "grey.100" }}>
              <TableCell>Loja</TableCell>
              <TableCell>ID LI</TableCell>
              <TableCell>Nome</TableCell>
              <TableCell>SKU</TableCell>
              <TableCell>Cód. fornecedor</TableCell>
              <TableCell>Fornecedor</TableCell>
              <TableCell>Status</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {initial.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} align="center">
                  Nenhum produto encontrado.
                </TableCell>
              </TableRow>
            ) : (
              initial.items.map((p) => (
                <TableRow key={p.id} hover>
                  <TableCell>{p.loja}</TableCell>
                  <TableCell>{p.produtoIdLojaIntegrada}</TableCell>
                  <TableCell>{p.nome}</TableCell>
                  <TableCell>{p.sku ?? "—"}</TableCell>
                  <TableCell>{p.codigoFornecedor ?? "—"}</TableCell>
                  <TableCell>
                    {p.fornecedorNome ? (
                      p.fornecedorNome
                    ) : (
                      <Chip label="Sem fornecedor" size="small" color="warning" variant="outlined" />
                    )}
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={p.ativo ? "Ativo" : "Inativo"}
                      size="small"
                      color={p.ativo ? "success" : "default"}
                      variant="outlined"
                    />
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>

      <PaginationBar page={page} totalPages={initial.totalPages} buildHref={buildHref} />
    </Box>
  );
}
