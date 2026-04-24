"use client";

import { useState } from "react";
import Paper from "@mui/material/Paper";
import TextField from "@mui/material/TextField";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Typography from "@mui/material/Typography";
import Alert from "@mui/material/Alert";
import { BRAND_NAME } from "@/lib/brand";

export function InviteForm({ onCreated }: { onCreated: () => void }) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    setLoading(true);
    try {
      const r = await fetch("/api/invite-user", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, password }),
      });
      if (!r.ok) {
        const t = await r.text();
        throw new Error(t || r.statusText);
      }
      setEmail("");
      setPassword("");
      onCreated();
    } catch (e) {
      setErr(String(e));
    } finally {
      setLoading(false);
    }
  }

  return (
    <Paper variant="outlined" sx={{ p: 2, mb: 2, bgcolor: "grey.50" }} component="form" onSubmit={submit}>
      <Typography variant="subtitle1" color="primary" gutterBottom sx={{ fontWeight: 700 }}>
        Criar utilizador (Supabase Auth)
      </Typography>
      <Typography variant="body2" color="text.secondary" component="p" sx={{ mb: 2 }}>
        Requer <code>SUPABASE_SERVICE_ROLE_KEY</code> no Vercel e o teu e-mail de admin em{" "}
        <code>ADMIN_INVITE_EMAILS</code> (convite {BRAND_NAME}).
      </Typography>
      <Box
        sx={{
          display: "flex",
          flexDirection: { xs: "column", sm: "row" },
          flexWrap: "wrap",
          gap: 2,
          alignItems: { xs: "stretch", sm: "flex-end" },
        }}
      >
        <TextField
          label="E-mail"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          size="small"
          sx={{ minWidth: 220, flex: "1 1 200px" }}
        />
        <TextField
          label="Senha inicial"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          slotProps={{ htmlInput: { minLength: 6 } }}
          required
          size="small"
          sx={{ minWidth: 200, flex: "1 1 180px" }}
        />
        <Button type="submit" variant="contained" color="primary" disabled={loading} sx={{ fontWeight: 700, alignSelf: { xs: "flex-start", sm: "auto" } }}>
          {loading ? "A criar…" : "Criar"}
        </Button>
      </Box>
      {err && (
        <Alert severity="error" sx={{ mt: 2 }}>
          {err}
        </Alert>
      )}
    </Paper>
  );
}
