"use client";

import { useState } from "react";
import { createClient } from "@/lib/supabase/client";
import { apiPatch } from "@/lib/api";
import { InviteForm } from "./invite-form";
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

type U = { id: string; email: string; nome: string | null; perfil: string; ativo: boolean; criadoEm: string };

export function UsuariosClient({ initial }: { initial: U[] }) {
  const [list, setList] = useState(initial);
  const [msg, setMsg] = useState<string | null>(null);

  async function patch(id: string, body: { perfil?: string; ativo?: boolean }) {
    setMsg(null);
    try {
      const supabase = createClient();
      const {
        data: { session },
      } = await supabase.auth.getSession();
      if (!session?.access_token) throw new Error("Sessão");
      const u = await apiPatch<U>(`/api/admin/usuarios/${id}`, session.access_token, body);
      setList((prev) => prev.map((x) => (x.id === id ? { ...x, ...u } : x)));
      setMsg("Guardado");
    } catch (e) {
      setMsg(String(e));
    }
  }

  return (
    <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
      <InviteForm
        onCreated={() => {
          window.location.reload();
        }}
      />
      {msg && <Typography color="text.secondary">{msg}</Typography>}
      <TableContainer component={Paper} variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow sx={{ bgcolor: "grey.100" }}>
              <TableCell>E-mail</TableCell>
              <TableCell>Perfil</TableCell>
              <TableCell>Ativo</TableCell>
              <TableCell width={360}>Ações</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {list.map((r) => (
              <TableRow key={r.id} hover>
                <TableCell>{r.email}</TableCell>
                <TableCell>{r.perfil}</TableCell>
                <TableCell>{r.ativo ? "Sim" : "Não"}</TableCell>
                <TableCell>
                  <Box sx={{ display: "flex", flexDirection: "row", flexWrap: "wrap", gap: 1 }}>
                    {r.perfil === "Operador" ? (
                      <Button
                        type="button"
                        size="small"
                        variant="outlined"
                        onClick={() => patch(r.id, { perfil: "Admin" })}
                      >
                        Tornar admin
                      </Button>
                    ) : (
                      <Button
                        type="button"
                        size="small"
                        variant="outlined"
                        onClick={() => patch(r.id, { perfil: "Operador" })}
                      >
                        Tornar operador
                      </Button>
                    )}
                    <Button type="button" size="small" variant="outlined" onClick={() => patch(r.id, { ativo: !r.ativo })}>
                      {r.ativo ? "Desativar" : "Ativar"}
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
