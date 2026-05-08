"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { createClient } from "@/lib/supabase/client";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import IconButton from "@mui/material/IconButton";
import TextField from "@mui/material/TextField";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import ReceiptLongOutlinedIcon from "@mui/icons-material/ReceiptLongOutlined";
import DeleteOutlineOutlinedIcon from "@mui/icons-material/DeleteOutlineOutlined";

export type ManualPedidoActionsRow = {
  pedidoId: number;
  pedidoItemId: number;
  dataPedido: string;
  pedidoExternoId: string;
  nomeCliente: string;
  clienteCpf: string | null;
  tipoEnvio: string | null;
  formaPagamento: string | null;
  valorFrete: number;
  produto: string;
  cor: string | null;
  tamanho: string | null;
  quantidade: number;
};

type Props = {
  row: ManualPedidoActionsRow;
  /** Primeira linha deste pedido na página — mostra editar pedido e excluir. */
  showPedidoWideActions: boolean;
};

function toDatetimeLocalValue(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return "";
  const x = new Date(d);
  x.setMinutes(x.getMinutes() - x.getTimezoneOffset());
  return x.toISOString().slice(0, 16);
}

export function ManualPedidoActions({ row, showPedidoWideActions }: Props) {
  const router = useRouter();
  const [openItem, setOpenItem] = useState(false);
  const [openPedido, setOpenPedido] = useState(false);
  const [loading, setLoading] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  const [produto, setProduto] = useState(row.produto);
  const [cor, setCor] = useState(row.cor ?? "");
  const [tamanho, setTamanho] = useState(row.tamanho ?? "");
  const [quantidade, setQuantidade] = useState(String(row.quantidade > 0 ? row.quantidade : 1));

  const [pedidoExternoId, setPedidoExternoId] = useState(row.pedidoExternoId);
  const [nomeCliente, setNomeCliente] = useState(row.nomeCliente);
  const [clienteCpf, setClienteCpf] = useState(row.clienteCpf ?? "");
  const [dataPedidoLocal, setDataPedidoLocal] = useState(toDatetimeLocalValue(row.dataPedido));
  const [tipoEnvio, setTipoEnvio] = useState(row.tipoEnvio ?? "");
  const [formaPagamento, setFormaPagamento] = useState(row.formaPagamento ?? "");
  const [valorFrete, setValorFrete] = useState(String(row.valorFrete));

  function resetItemForm() {
    setProduto(row.produto);
    setCor(row.cor ?? "");
    setTamanho(row.tamanho ?? "");
    setQuantidade(String(row.quantidade > 0 ? row.quantidade : 1));
  }

  function resetPedidoForm() {
    setPedidoExternoId(row.pedidoExternoId);
    setNomeCliente(row.nomeCliente);
    setClienteCpf(row.clienteCpf ?? "");
    setDataPedidoLocal(toDatetimeLocalValue(row.dataPedido));
    setTipoEnvio(row.tipoEnvio ?? "");
    setFormaPagamento(row.formaPagamento ?? "");
    setValorFrete(String(row.valorFrete));
  }

  async function getToken() {
    const supabase = createClient();
    const {
      data: { session },
    } = await supabase.auth.getSession();
    const token = session?.access_token;
    if (!token) throw new Error("Sessão expirou. Volte a entrar.");
    return token;
  }

  async function saveItem() {
    setErr(null);
    setLoading(true);
    try {
      const token = await getToken();
      const q = parseInt(quantidade, 10);
      const body = {
        produto: produto.trim(),
        cor: cor.trim() || null,
        tamanho: tamanho.trim() || null,
        quantidade: Number.isFinite(q) && q > 0 ? q : 1,
      };
      const r = await fetch(`/api/pedidos/${row.pedidoId}/itens/${row.pedidoItemId}`, {
        method: "PUT",
        headers: {
          Authorization: `Bearer ${token}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify(body),
      });
      if (!r.ok) {
        const t = await r.text();
        throw new Error(t || `HTTP ${r.status}`);
      }
      setOpenItem(false);
      router.refresh();
    } catch (e) {
      setErr(String(e instanceof Error ? e.message : e));
    } finally {
      setLoading(false);
    }
  }

  async function savePedido() {
    setErr(null);
    setLoading(true);
    try {
      const token = await getToken();
      const vf = parseFloat(valorFrete.replace(",", "."));
      const body = {
        pedidoExternoId: pedidoExternoId.trim(),
        nomeCliente: nomeCliente.trim(),
        clienteCpf: clienteCpf.trim() || null,
        dataPedido: new Date(dataPedidoLocal).toISOString(),
        tipoEnvio: tipoEnvio.trim() || null,
        formaPagamento: formaPagamento.trim() || null,
        valorFrete: Number.isFinite(vf) ? vf : 0,
      };
      const r = await fetch(`/api/pedidos/${row.pedidoId}`, {
        method: "PUT",
        headers: {
          Authorization: `Bearer ${token}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify(body),
      });
      if (!r.ok) {
        const t = await r.text();
        throw new Error(t || `HTTP ${r.status}`);
      }
      setOpenPedido(false);
      router.refresh();
    } catch (e) {
      setErr(String(e instanceof Error ? e.message : e));
    } finally {
      setLoading(false);
    }
  }

  async function excluirPedido() {
    if (
      !window.confirm(
        `Excluir o pedido "${row.pedidoExternoId}" e todos os seus itens? Esta acção não pode ser anulada.`
      )
    ) {
      return;
    }
    setErr(null);
    setLoading(true);
    try {
      const token = await getToken();
      const r = await fetch(`/api/pedidos/${row.pedidoId}`, {
        method: "DELETE",
        headers: { Authorization: `Bearer ${token}` },
      });
      if (!r.ok) {
        const t = await r.text();
        throw new Error(t || `HTTP ${r.status}`);
      }
      router.refresh();
    } catch (e) {
      window.alert(String(e instanceof Error ? e.message : e));
    } finally {
      setLoading(false);
    }
  }

  return (
    <Box sx={{ display: "inline-flex", gap: 0.25, mr: 0.5, verticalAlign: "middle" }}>
      <Tooltip title="Editar item desta linha">
        <IconButton
          type="button"
          size="small"
          color="primary"
          aria-label="Editar item"
          disabled={loading}
          onClick={(e) => {
            e.stopPropagation();
            resetItemForm();
            setErr(null);
            setOpenItem(true);
          }}
        >
          <EditOutlinedIcon fontSize="small" />
        </IconButton>
      </Tooltip>
      {showPedidoWideActions && (
        <>
          <Tooltip title="Editar dados do pedido (cabeçalho)">
            <IconButton
              type="button"
              size="small"
              color="primary"
              aria-label="Editar pedido"
              disabled={loading}
              onClick={(e) => {
                e.stopPropagation();
                resetPedidoForm();
                setErr(null);
                setOpenPedido(true);
              }}
            >
              <ReceiptLongOutlinedIcon fontSize="small" />
            </IconButton>
          </Tooltip>
          <Tooltip title="Excluir pedido inteiro">
            <IconButton
              type="button"
              size="small"
              color="error"
              aria-label="Excluir pedido"
              disabled={loading}
              onClick={(e) => {
                e.stopPropagation();
                void excluirPedido();
              }}
            >
              <DeleteOutlineOutlinedIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        </>
      )}

      <Dialog open={openItem} onClose={() => !loading && setOpenItem(false)} fullWidth maxWidth="sm">
        <DialogTitle>Editar item</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: 1 }}>
          {err && (
            <Typography variant="body2" color="error">
              {err}
            </Typography>
          )}
          <TextField label="Produto" value={produto} onChange={(e) => setProduto(e.target.value)} required fullWidth />
          <TextField label="Cor" value={cor} onChange={(e) => setCor(e.target.value)} fullWidth />
          <TextField label="Tamanho" value={tamanho} onChange={(e) => setTamanho(e.target.value)} fullWidth />
          <TextField
            label="Quantidade (etiquetas)"
            type="number"
            slotProps={{ htmlInput: { min: 1 } }}
            value={quantidade}
            onChange={(e) => setQuantidade(e.target.value)}
            fullWidth
            helperText="Um valor maior que 1 cria cópias da linha como novas etiquetas pendentes."
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenItem(false)} disabled={loading}>
            Cancelar
          </Button>
          <Button variant="contained" onClick={() => void saveItem()} disabled={loading}>
            Guardar
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={openPedido} onClose={() => !loading && setOpenPedido(false)} fullWidth maxWidth="sm">
        <DialogTitle>Editar pedido manual</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: 1 }}>
          {err && (
            <Typography variant="body2" color="error">
              {err}
            </Typography>
          )}
          <TextField
            label="ID externo"
            value={pedidoExternoId}
            onChange={(e) => setPedidoExternoId(e.target.value)}
            required
            fullWidth
          />
          <TextField
            label="Nome do cliente"
            value={nomeCliente}
            onChange={(e) => setNomeCliente(e.target.value)}
            required
            fullWidth
          />
          <TextField label="CPF" value={clienteCpf} onChange={(e) => setClienteCpf(e.target.value)} fullWidth />
          <TextField
            label="Data do pedido"
            type="datetime-local"
            value={dataPedidoLocal}
            onChange={(e) => setDataPedidoLocal(e.target.value)}
            slotProps={{ inputLabel: { shrink: true } }}
            fullWidth
          />
          <TextField label="Tipo de envio" value={tipoEnvio} onChange={(e) => setTipoEnvio(e.target.value)} fullWidth />
          <TextField
            label="Forma de pagamento"
            value={formaPagamento}
            onChange={(e) => setFormaPagamento(e.target.value)}
            fullWidth
          />
          <TextField
            label="Valor do frete"
            type="number"
            slotProps={{ htmlInput: { step: "0.01", min: 0 } }}
            value={valorFrete}
            onChange={(e) => setValorFrete(e.target.value)}
            fullWidth
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenPedido(false)} disabled={loading}>
            Cancelar
          </Button>
          <Button variant="contained" onClick={() => void savePedido()} disabled={loading}>
            Guardar
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
