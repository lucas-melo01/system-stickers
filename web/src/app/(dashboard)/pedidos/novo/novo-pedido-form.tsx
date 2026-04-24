"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { createClient } from "@/lib/supabase/client";
import Link from "next/link";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import TextField from "@mui/material/TextField";
import MenuItem from "@mui/material/MenuItem";
import Typography from "@mui/material/Typography";
import Paper from "@mui/material/Paper";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import IconButton from "@mui/material/IconButton";
import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import Alert from "@mui/material/Alert";

type Linha = {
  produto: string;
  sku: string;
  cor: string;
  tamanho: string;
  quantidade: number;
  valorCusto: number;
  valorVenda: number;
};

const linhaVazia = (): Linha => ({
  produto: "",
  sku: "",
  cor: "",
  tamanho: "",
  quantidade: 1,
  valorCusto: 0,
  valorVenda: 0,
});

export function NovoPedidoForm() {
  const router = useRouter();
  const [err, setErr] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [pedidoExternoId, setPedidoExternoId] = useState("");
  const [dataPedido, setDataPedido] = useState(() => {
    const d = new Date();
    d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
    return d.toISOString().slice(0, 16);
  });
  const [nomeCliente, setNomeCliente] = useState("");
  const [clienteCpf, setClienteCpf] = useState("");
  const [vendedor, setVendedor] = useState("Manual");
  const [tipoEnvio, setTipoEnvio] = useState("");
  const [formaPagamento, setFormaPagamento] = useState("");
  const [valorFrete, setValorFrete] = useState(0);
  const [itens, setItens] = useState<Linha[]>([linhaVazia()]);

  function addLinha() {
    setItens((rows) => [...rows, linhaVazia()]);
  }

  function removeLinha(i: number) {
    setItens((rows) => (rows.length <= 1 ? rows : rows.filter((_, j) => j !== i)));
  }

  function setLinha(i: number, patch: Partial<Linha>) {
    setItens((rows) => rows.map((r, j) => (j === i ? { ...r, ...patch } : r)));
  }

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    const valid = itens.filter((x) => x.produto.trim().length > 0);
    if (valid.length === 0) {
      setErr("Inclua pelo menos um item com produto preenchido.");
      return;
    }
    setLoading(true);
    try {
      const supabase = createClient();
      const {
        data: { session },
      } = await supabase.auth.getSession();
      if (!session?.access_token) {
        setErr("Sessão expirou.");
        return;
      }
      const dataIso = new Date(dataPedido).toISOString();
      const body = {
        pedidoExternoId: pedidoExternoId.trim(),
        nomeCliente: nomeCliente.trim(),
        clienteCpf: clienteCpf.trim() || null,
        dataPedido: dataIso,
        vendedor: vendedor || "Manual",
        tipoEnvio: tipoEnvio || null,
        formaPagamento: formaPagamento || null,
        valorFrete,
        itens: valid.map((r) => ({
          produto: r.produto.trim(),
          sku: r.sku.trim() || null,
          cor: r.cor.trim() || null,
          tamanho: r.tamanho.trim() || null,
          quantidade: r.quantidade > 0 ? r.quantidade : 1,
          valorCusto: r.valorCusto,
          valorVenda: r.valorVenda,
        })),
      };
      const res = await fetch("/api/pedidos/create", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${session.access_token}`,
        },
        body: JSON.stringify(body),
      });
      if (!res.ok) {
        const t = await res.text();
        throw new Error(t || res.statusText);
      }
      router.push("/pedidos");
      router.refresh();
    } catch (e) {
      setErr(String(e));
    } finally {
      setLoading(false);
    }
  }

  return (
    <Box component="form" onSubmit={onSubmit} noValidate>
      <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 2, flexWrap: "wrap", mb: 2 }}>
        <Typography variant="h5" color="primary" sx={{ fontWeight: 700 }}>
          Adicionar pedido manual
        </Typography>
        <Button component={Link} href="/pedidos" variant="outlined" size="small">
          Voltar à listagem
        </Button>
      </Box>
      {err && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {err}
        </Alert>
      )}
      <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }} gutterBottom>
          Dados do pedido
        </Typography>
        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: { xs: "1fr", sm: "1fr 1fr" },
            gap: 2,
            mb: 2,
          }}
        >
          <TextField
            required
            label="ID externo do pedido"
            value={pedidoExternoId}
            onChange={(e) => setPedidoExternoId(e.target.value)}
            size="small"
          />
          <TextField
            required
            type="datetime-local"
            label="Data do pedido"
            value={dataPedido}
            onChange={(e) => setDataPedido(e.target.value)}
            size="small"
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <TextField
            required
            label="Nome do cliente"
            value={nomeCliente}
            onChange={(e) => setNomeCliente(e.target.value)}
            size="small"
          />
          <TextField
            label="CPF do cliente"
            value={clienteCpf}
            onChange={(e) => setClienteCpf(e.target.value)}
            placeholder="000.000.000-00"
            size="small"
          />
          <TextField select label="Vendedor" value={vendedor} onChange={(e) => setVendedor(e.target.value)} size="small">
            <MenuItem value="Resume">Resume</MenuItem>
            <MenuItem value="DonnaKora">DonnaKora</MenuItem>
            <MenuItem value="Manual">Manual</MenuItem>
          </TextField>
          <TextField select label="Tipo de envio" value={tipoEnvio} onChange={(e) => setTipoEnvio(e.target.value)} size="small">
            <MenuItem value="">—</MenuItem>
            <MenuItem value="PAC">PAC</MenuItem>
            <MenuItem value="SEDEX">SEDEX</MenuItem>
            <MenuItem value="Outro">Outro</MenuItem>
          </TextField>
          <TextField
            label="Forma de pagamento"
            value={formaPagamento}
            onChange={(e) => setFormaPagamento(e.target.value)}
            size="small"
            placeholder="Ex.: Pix"
          />
          <TextField
            type="number"
            label="Valor do frete (R$)"
            value={valorFrete}
            onChange={(e) => setValorFrete(parseFloat(e.target.value) || 0)}
            size="small"
            slotProps={{ htmlInput: { min: 0, step: 0.01 } }}
          />
        </Box>
      </Paper>
      <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
        <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", mb: 1 }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
            Itens do pedido
          </Typography>
          <Button type="button" startIcon={<AddIcon />} size="small" onClick={addLinha} variant="outlined">
            Adicionar item
          </Button>
        </Box>
        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow sx={{ bgcolor: "grey.100" }}>
                <TableCell>Produto *</TableCell>
                <TableCell>SKU</TableCell>
                <TableCell>Cor</TableCell>
                <TableCell>Tam.</TableCell>
                <TableCell width={90}>Qtd</TableCell>
                <TableCell width={100}>R$ custo</TableCell>
                <TableCell width={100}>R$ venda</TableCell>
                <TableCell width={56} />
              </TableRow>
            </TableHead>
            <TableBody>
              {itens.map((row, i) => (
                <TableRow key={i}>
                  <TableCell>
                    <TextField
                      required
                      size="small"
                      fullWidth
                      value={row.produto}
                      onChange={(e) => setLinha(i, { produto: e.target.value })}
                    />
                  </TableCell>
                  <TableCell>
                    <TextField
                      size="small"
                      fullWidth
                      value={row.sku}
                      onChange={(e) => setLinha(i, { sku: e.target.value })}
                    />
                  </TableCell>
                  <TableCell>
                    <TextField
                      size="small"
                      fullWidth
                      value={row.cor}
                      onChange={(e) => setLinha(i, { cor: e.target.value })}
                    />
                  </TableCell>
                  <TableCell>
                    <TextField
                      size="small"
                      fullWidth
                      value={row.tamanho}
                      onChange={(e) => setLinha(i, { tamanho: e.target.value })}
                    />
                  </TableCell>
                  <TableCell>
                    <TextField
                      type="number"
                      size="small"
                      fullWidth
                      value={row.quantidade}
                      onChange={(e) => setLinha(i, { quantidade: parseInt(e.target.value, 10) || 1 })}
                      slotProps={{ htmlInput: { min: 1 } }}
                    />
                  </TableCell>
                  <TableCell>
                    <TextField
                      type="number"
                      size="small"
                      fullWidth
                      value={row.valorCusto}
                      onChange={(e) => setLinha(i, { valorCusto: parseFloat(e.target.value) || 0 })}
                      slotProps={{ htmlInput: { min: 0, step: 0.01 } }}
                    />
                  </TableCell>
                  <TableCell>
                    <TextField
                      type="number"
                      size="small"
                      fullWidth
                      value={row.valorVenda}
                      onChange={(e) => setLinha(i, { valorVenda: parseFloat(e.target.value) || 0 })}
                      slotProps={{ htmlInput: { min: 0, step: 0.01 } }}
                    />
                  </TableCell>
                  <TableCell>
                    <IconButton type="button" onClick={() => removeLinha(i)} aria-label="Remover" size="small" disabled={itens.length <= 1}>
                      <DeleteIcon fontSize="small" />
                    </IconButton>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </Paper>
      <Button type="submit" variant="contained" color="primary" size="large" disabled={loading} sx={{ fontWeight: 800 }}>
        {loading ? "A guardar…" : "Guardar pedido"}
      </Button>
    </Box>
  );
}
