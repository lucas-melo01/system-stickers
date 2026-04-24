"use client";

import { useState } from "react";
import { createClient } from "@/lib/supabase/client";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Paper from "@mui/material/Paper";
import Button from "@mui/material/Button";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import TextField from "@mui/material/TextField";
import MenuItem from "@mui/material/MenuItem";
import Alert from "@mui/material/Alert";
import { BRAND_NAME } from "@/lib/brand";
import { isAdminPerfil } from "@/lib/is-admin-perfil";

type U = { id: string; email: string; nome: string | null; perfil: string | number; ativo: boolean; criadoEm: string };

export function GestaoUtilizadoresClient({ initial }: { initial: U[] }) {
  const [list, setList] = useState(initial);
  const [msg, setMsg] = useState<string | null>(null);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [perfilNovo, setPerfilNovo] = useState<"Operador" | "Admin">("Operador");
  const [criando, setCriando] = useState(false);
  const [errCriar, setErrCriar] = useState<string | null>(null);

  async function patch(id: string, body: { perfil?: string; ativo?: boolean }) {
    setMsg(null);
    try {
      const supabase = createClient();
      const {
        data: { session },
      } = await supabase.auth.getSession();
      if (!session?.access_token) throw new Error("Sessão");
      const r = await fetch(`/api/admin/usuarios/${id}`, {
        method: "PATCH",
        headers: {
          Authorization: `Bearer ${session.access_token}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify(body),
      });
      const text = await r.text();
      if (!r.ok) throw new Error(text || r.statusText);
      const u = JSON.parse(text) as U;
      setList((prev) => prev.map((x) => (x.id === id ? { ...x, ...u } : x)));
      setMsg("Guardado");
    } catch (e) {
      setMsg(String(e));
    }
  }

  async function criar(e: React.FormEvent) {
    e.preventDefault();
    setErrCriar(null);
    setCriando(true);
    try {
      const r = await fetch("/api/invite-user", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, password, perfil: perfilNovo }),
      });
      const t = await r.text();
      if (!r.ok) throw new Error(t || r.statusText);
      setEmail("");
      setPassword("");
      setMsg("Utilizador criado. A lista pode demorar a actualizar; actualize a página se necessário.");
      window.location.reload();
    } catch (e) {
      setErrCriar(String(e));
    } finally {
      setCriando(false);
    }
  }

  return (
    <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
      <Paper variant="outlined" component="form" onSubmit={criar} sx={{ p: 2, bgcolor: "grey.50" }}>
        <Typography variant="subtitle1" color="primary" sx={{ fontWeight: 700 }} gutterBottom>
          Novo utilizador ({BRAND_NAME})
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Cria a conta no Supabase Auth. O perfil fica guardado na API (requer a API alcançável e JWT configurado). Se
          tiveres <code>SUPABASE_SERVICE_ROLE_KEY</code> no Vercel, o convite funciona a partir desta tabela.
        </Typography>
        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: { xs: "1fr", sm: "1fr 1fr auto auto" },
            gap: 2,
            alignItems: "flex-end",
          }}
        >
          <TextField
            label="E-mail"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            size="small"
          />
          <TextField
            label="Senha inicial"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            size="small"
            slotProps={{ htmlInput: { minLength: 6 } }}
          />
          <TextField select label="Perfil" value={perfilNovo} onChange={(e) => setPerfilNovo(e.target.value as "Admin" | "Operador")} size="small" sx={{ minWidth: 140 }}>
            <MenuItem value="Operador">Operador</MenuItem>
            <MenuItem value="Admin">Administrador</MenuItem>
          </TextField>
          <Button type="submit" variant="contained" color="primary" disabled={criando} sx={{ fontWeight: 700, height: 40 }}>
            {criando ? "A criar…" : "Criar"}
          </Button>
        </Box>
        {errCriar && (
          <Alert severity="error" sx={{ mt: 2 }}>
            {errCriar}
          </Alert>
        )}
      </Paper>

      {msg && <Typography color="text.secondary">{msg}</Typography>}

      <TableContainer component={Paper} variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow sx={{ bgcolor: "grey.100" }}>
              <TableCell>E-mail</TableCell>
              <TableCell>Perfil</TableCell>
              <TableCell>Estado</TableCell>
              <TableCell width={280}>Acções</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {list.map((r) => (
              <TableRow key={r.id} hover>
                <TableCell>{r.email}</TableCell>
                <TableCell>
                  {isAdminPerfil(r.perfil) ? "Admin" : "Operador"}
                </TableCell>
                <TableCell>{r.ativo ? "Activo" : "Inactivo"}</TableCell>
                <TableCell>
                  <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1 }}>
                    {!isAdminPerfil(r.perfil) ? (
                      <Button type="button" size="small" variant="outlined" onClick={() => patch(r.id, { perfil: "Admin" })}>
                        Tornar admin
                      </Button>
                    ) : (
                      <Button type="button" size="small" variant="outlined" onClick={() => patch(r.id, { perfil: "Operador" })}>
                        Tornar operador
                      </Button>
                    )}
                    <Button
                      type="button"
                      size="small"
                      color={r.ativo ? "warning" : "success"}
                      variant="outlined"
                      onClick={() => patch(r.id, { ativo: !r.ativo })}
                    >
                      {r.ativo ? "Inactivar" : "Activar"}
                    </Button>
                  </Box>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  );
}
