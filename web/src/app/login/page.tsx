"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { createClient } from "@/lib/supabase/client";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import Paper from "@mui/material/Paper";
import Alert from "@mui/material/Alert";
import { BRAND_NAME, APP_SHORT_TITLE } from "@/lib/brand";
import StorefrontIcon from "@mui/icons-material/Storefront";

export default function LoginPage() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [err, setErr] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const router = useRouter();
  const configOk =
    typeof process.env.NEXT_PUBLIC_SUPABASE_URL === "string" &&
    process.env.NEXT_PUBLIC_SUPABASE_URL.length > 0 &&
    typeof process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY === "string" &&
    process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY.length > 0;

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!configOk) {
      setErr(
        "Aplicação sem variáveis Supabase no Vercel. Adicione NEXT_PUBLIC_SUPABASE_URL e NEXT_PUBLIC_SUPABASE_ANON_KEY e faça redeploy."
      );
      return;
    }
    setErr(null);
    setLoading(true);
    const supabase = createClient();
    const { data, error } = await supabase.auth.signInWithPassword({ email, password });
    setLoading(false);
    if (error) {
      setErr(error.message);
      return;
    }
    if (data.session) {
      router.replace("/pedidos");
      router.refresh();
    }
  }

  return (
    <Box
      sx={{
        minHeight: "100vh",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        p: 2,
        bgcolor: "primary.main",
      }}
    >
      <Paper elevation={4} sx={{ p: 4, maxWidth: 400, width: 1, borderRadius: 2 }}>
        <Box sx={{ display: "flex", alignItems: "center", gap: 1.5, mb: 2 }}>
          <StorefrontIcon color="primary" fontSize="large" />
          <Box>
            <Typography variant="h5" color="primary" sx={{ fontWeight: 800 }}>
              {BRAND_NAME}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {APP_SHORT_TITLE}
            </Typography>
          </Box>
        </Box>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Acesse com a conta do Supabase Auth.
        </Typography>
        {!configOk && (
          <Alert severity="warning" sx={{ mb: 2 }}>
            O deploy não tem <code>NEXT_PUBLIC_SUPABASE_URL</code> e <code>NEXT_PUBLIC_SUPABASE_ANON_KEY</code> (ou
            faltou rebuild).
          </Alert>
        )}
        <form onSubmit={onSubmit}>
          {err && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {err}
            </Alert>
          )}
          <TextField
            fullWidth
            label="E-mail"
            name="email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            autoComplete="email"
            margin="normal"
            size="small"
          />
          <TextField
            fullWidth
            label="Senha"
            name="password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="current-password"
            margin="normal"
            size="small"
          />
          <Button
            type="submit"
            fullWidth
            variant="contained"
            color="secondary"
            disabled={loading || !configOk}
            sx={{ mt: 2, fontWeight: 800 }}
            size="large"
          >
            {loading ? "Entrando…" : "Entrar"}
          </Button>
        </form>
      </Paper>
    </Box>
  );
}
