"use client";

import { useCallback, useEffect, useState } from "react";
import { createClient } from "@/lib/supabase/client";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import Paper from "@mui/material/Paper";
import TextField from "@mui/material/TextField";
import Button from "@mui/material/Button";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Dialog from "@mui/material/Dialog";
import DialogTitle from "@mui/material/DialogTitle";
import DialogContent from "@mui/material/DialogContent";
import DialogActions from "@mui/material/DialogActions";
import Autocomplete from "@mui/material/Autocomplete";
import Chip from "@mui/material/Chip";
import Alert from "@mui/material/Alert";
import IconButton from "@mui/material/IconButton";
import EditIcon from "@mui/icons-material/Edit";
import AddIcon from "@mui/icons-material/Add";
import MenuItem from "@mui/material/MenuItem";
import { PaginationBar } from "@/components/PaginationBar";
import type { FornecedorLista, Paged, ProdutoLista } from "@/lib/normalize-cadastros";
import { normalizeFornecedor, normalizePaged, normalizeProduto } from "@/lib/normalize-cadastros";

async function authFetch(path: string, init?: RequestInit) {
  const supabase = createClient();
  const {
    data: { session },
  } = await supabase.auth.getSession();
  if (!session?.access_token) throw new Error("Sessão expirada");
  const r = await fetch(path, {
    ...init,
    headers: {
      ...(init?.headers ?? {}),
      Authorization: `Bearer ${session.access_token}`,
      "Content-Type": "application/json",
    },
  });
  const text = await r.text();
  if (!r.ok) throw new Error(text || r.statusText);
  return text ? JSON.parse(text) : null;
}

export function FornecedoresClient({
  initial,
  q,
  page,
}: {
  initial: Paged<FornecedorLista>;
  q?: string;
  page: number;
}) {
  const [list, setList] = useState(initial.items);
  const [msg, setMsg] = useState<string | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editId, setEditId] = useState<number | null>(null);
  const [nome, setNome] = useState("");
  const [email, setEmail] = useState("");
  const [whatsApp, setWhatsApp] = useState("");
  const [ativo, setAtivo] = useState(true);
  const [salvando, setSalvando] = useState(false);
  const [produtosOpcoes, setProdutosOpcoes] = useState<ProdutoLista[]>([]);
  const [produtosSel, setProdutosSel] = useState<ProdutoLista[]>([]);
  const [filtroLojaProd, setFiltroLojaProd] = useState("");
  const [buscaProd, setBuscaProd] = useState("");
  const [carregandoProd, setCarregandoProd] = useState(false);

  const buildHref = (p: number) => {
    const qs = new URLSearchParams();
    if (q) qs.set("q", q);
    qs.set("page", String(p));
    return `/cadastros/fornecedores?${qs.toString()}`;
  };

  const carregarProdutos = useCallback(async () => {
    setCarregandoProd(true);
    try {
      const qs = new URLSearchParams({ pageSize: "100", page: "1" });
      if (filtroLojaProd) qs.set("loja", filtroLojaProd);
      if (buscaProd) qs.set("q", buscaProd);
      const raw = await authFetch(`/api/produtos?${qs.toString()}`);
      const paged = normalizePaged(raw, normalizeProduto);
      setProdutosOpcoes(paged.items);
    } catch (e) {
      setErr(String(e));
    } finally {
      setCarregandoProd(false);
    }
  }, [filtroLojaProd, buscaProd]);

  useEffect(() => {
    if (dialogOpen) void carregarProdutos();
  }, [dialogOpen, carregarProdutos]);

  function abrirNovo() {
    setEditId(null);
    setNome("");
    setEmail("");
    setWhatsApp("");
    setAtivo(true);
    setProdutosSel([]);
    setErr(null);
    setDialogOpen(true);
  }

  async function abrirEditar(f: FornecedorLista) {
    setEditId(f.id);
    setNome(f.nomeRazaoSocial);
    setEmail(f.email ?? "");
    setWhatsApp(f.whatsApp);
    setAtivo(f.ativo);
    setErr(null);
    setDialogOpen(true);
    try {
      const vinc = (await authFetch(`/api/fornecedores/${f.id}/produtos`)) as { produtoIds?: number[] };
      const ids = new Set(vinc?.produtoIds ?? []);
      const qs = new URLSearchParams({ pageSize: "100", page: "1" });
      const raw = await authFetch(`/api/produtos?${qs.toString()}`);
      const paged = normalizePaged(raw, normalizeProduto);
      setProdutosOpcoes(paged.items);
      setProdutosSel(paged.items.filter((p) => ids.has(p.id)));
    } catch (e) {
      setErr(String(e));
      setProdutosSel([]);
    }
  }

  async function salvar() {
    setSalvando(true);
    setErr(null);
    try {
      const body = {
        nomeRazaoSocial: nome,
        email: email || null,
        whatsApp,
        ativo,
      };
      let fornecedorId = editId;
      if (editId) {
        await authFetch(`/api/fornecedores/${editId}`, { method: "PUT", body: JSON.stringify(body) });
      } else {
        const criado = await authFetch("/api/fornecedores", {
          method: "POST",
          body: JSON.stringify({ ...body, produtoIds: [] }),
        });
        fornecedorId = normalizeFornecedor(criado as Record<string, unknown>).id;
      }
      if (fornecedorId) {
        await authFetch(`/api/fornecedores/${fornecedorId}/produtos`, {
          method: "PUT",
          body: JSON.stringify({ produtoIds: produtosSel.map((p) => p.id) }),
        });
      }
      setDialogOpen(false);
      setMsg(editId ? "Fornecedor atualizado." : "Fornecedor criado.");
      window.location.reload();
    } catch (e) {
      setErr(String(e));
    } finally {
      setSalvando(false);
    }
  }

  async function excluir(id: number) {
    if (!confirm("Desativar ou excluir este fornecedor?")) return;
    try {
      await authFetch(`/api/fornecedores/${id}`, { method: "DELETE" });
      setList((prev) => prev.filter((x) => x.id !== id));
      setMsg("Fornecedor removido/desativado.");
    } catch (e) {
      setErr(String(e));
    }
  }

  return (
    <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
      <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "center", flexWrap: "wrap", gap: 1 }}>
        <Typography variant="h5" color="primary" sx={{ fontWeight: 700 }}>
          Fornecedores
        </Typography>
        <Button variant="contained" color="secondary" startIcon={<AddIcon />} onClick={abrirNovo}>
          Novo fornecedor
        </Button>
      </Box>
      <Typography variant="body2" color="text.secondary">
        Cadastre fornecedores e vincule os produtos que cada um atende. Esse vínculo é usado nas notificações de encomenda.
      </Typography>

      {msg && <Alert severity="success" onClose={() => setMsg(null)}>{msg}</Alert>}
      {err && !dialogOpen && <Alert severity="error" onClose={() => setErr(null)}>{err}</Alert>}

      <Paper variant="outlined" component="form" method="get" sx={{ p: 2 }}>
        <Box sx={{ display: "flex", gap: 2, alignItems: "flex-end", flexWrap: "wrap" }}>
          <TextField name="q" label="Buscar" defaultValue={q ?? ""} size="small" sx={{ minWidth: 220 }} />
          <Button type="submit" variant="contained" color="primary">
            Filtrar
          </Button>
        </Box>
      </Paper>

      <TableContainer component={Paper} variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow sx={{ bgcolor: "grey.100" }}>
              <TableCell>Nome / Razão Social</TableCell>
              <TableCell>E-mail</TableCell>
              <TableCell>WhatsApp</TableCell>
              <TableCell>Produtos</TableCell>
              <TableCell>Status</TableCell>
              <TableCell align="right">Ações</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {list.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} align="center">
                  Nenhum fornecedor cadastrado.
                </TableCell>
              </TableRow>
            ) : (
              list.map((f) => (
                <TableRow key={f.id} hover>
                  <TableCell>{f.nomeRazaoSocial}</TableCell>
                  <TableCell>{f.email ?? "—"}</TableCell>
                  <TableCell>{f.whatsApp}</TableCell>
                  <TableCell>{f.produtosVinculados}</TableCell>
                  <TableCell>
                    <Chip label={f.ativo ? "Ativo" : "Inativo"} size="small" color={f.ativo ? "success" : "default"} variant="outlined" />
                  </TableCell>
                  <TableCell align="right">
                    <IconButton size="small" onClick={() => abrirEditar(f)} aria-label="Editar">
                      <EditIcon fontSize="small" />
                    </IconButton>
                    <Button size="small" color="error" onClick={() => excluir(f.id)}>
                      Excluir
                    </Button>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>

      <PaginationBar page={page} totalPages={initial.totalPages} buildHref={buildHref} />

      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>{editId ? "Editar fornecedor" : "Novo fornecedor"}</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: 1 }}>
          {err && <Alert severity="error">{err}</Alert>}
          <TextField label="Nome / Razão Social" value={nome} onChange={(e) => setNome(e.target.value)} required fullWidth />
          <TextField label="E-mail" type="email" value={email} onChange={(e) => setEmail(e.target.value)} fullWidth />
          <TextField
            label="WhatsApp (somente números, com DDI)"
            value={whatsApp}
            onChange={(e) => setWhatsApp(e.target.value)}
            required
            fullWidth
            placeholder="5511999998888"
          />
          <TextField select label="Status" value={ativo ? "1" : "0"} onChange={(e) => setAtivo(e.target.value === "1")} fullWidth>
            <MenuItem value="1">Ativo</MenuItem>
            <MenuItem value="0">Inativo</MenuItem>
          </TextField>

          <Typography variant="subtitle2" sx={{ mt: 1, fontWeight: 700 }}>
            Produtos vinculados
          </Typography>
          <Box sx={{ display: "flex", gap: 1, flexWrap: "wrap" }}>
            <TextField
              select
              label="Filtrar loja"
              value={filtroLojaProd}
              onChange={(e) => setFiltroLojaProd(e.target.value)}
              size="small"
              sx={{ minWidth: 160 }}
            >
              <MenuItem value="">Todas</MenuItem>
              <MenuItem value="Resume Modas">Resume Modas</MenuItem>
              <MenuItem value="DonnaKora">DonnaKora</MenuItem>
            </TextField>
            <TextField
              label="Buscar produto"
              value={buscaProd}
              onChange={(e) => setBuscaProd(e.target.value)}
              size="small"
              sx={{ flex: 1, minWidth: 160 }}
            />
            <Button variant="outlined" onClick={() => void carregarProdutos()} disabled={carregandoProd}>
              {carregandoProd ? "…" : "Atualizar lista"}
            </Button>
          </Box>
          <Autocomplete
            multiple
            options={produtosOpcoes}
            value={produtosSel}
            onChange={(_, v) => setProdutosSel(v)}
            getOptionLabel={(o) => `${o.nome} (${o.loja})`}
            isOptionEqualToValue={(a, b) => a.id === b.id}
            loading={carregandoProd}
            renderInput={(params) => (
              <TextField {...params} label="Selecione os produtos deste fornecedor" placeholder="Buscar…" />
            )}
            renderTags={(value, getTagProps) =>
              value.map((option, index) => (
                <Chip {...getTagProps({ index })} key={option.id} label={option.nome} size="small" />
              ))
            }
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogOpen(false)}>Cancelar</Button>
          <Button variant="contained" onClick={() => void salvar()} disabled={salvando || !nome.trim() || !whatsApp.trim()}>
            {salvando ? "Salvando…" : "Salvar"}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
