"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { createClient } from "@/lib/supabase/client";

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
      setErr("Aplicação sem variáveis Supabase no Vercel. Adicione NEXT_PUBLIC_SUPABASE_URL e NEXT_PUBLIC_SUPABASE_ANON_KEY e faça redeploy.");
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
      const token = data.session.access_token;
      const api = process.env.NEXT_PUBLIC_API_URL;
      if (api) {
        try {
          await fetch(`${api}/api/auth/sync`, {
            method: "POST",
            headers: { Authorization: `Bearer ${token}` },
          });
        } catch {
          // API pode não ter JWT ainda; login Supabase ainda funciona
        }
      }
      router.replace("/pedidos");
      router.refresh();
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-[#001623] p-4">
      <form
        onSubmit={onSubmit}
        className="w-full max-w-sm border border-zinc-700 bg-zinc-900/50 p-8 rounded-xl shadow-lg"
      >
        <h1 className="text-2xl font-bold text-[#FFF200] mb-6">Sistema Etiquetas</h1>
        <p className="text-zinc-400 text-sm mb-6">Acesse com a conta do Supabase Auth.</p>
        {!configOk && (
          <p className="text-amber-400 text-sm mb-4 border border-amber-800 rounded p-3">
            O deploy não tem <code className="text-amber-200">NEXT_PUBLIC_SUPABASE_URL</code> nem{" "}
            <code className="text-amber-200">NEXT_PUBLIC_SUPABASE_ANON_KEY</code> no Vercel, ou faltou um novo
            build depois de as definir.
          </p>
        )}
        {err && <p className="text-red-400 text-sm mb-4">{err}</p>}
        <label className="block text-zinc-300 text-sm mb-1">E-mail</label>
        <input
          className="w-full mb-4 px-3 py-2 rounded bg-zinc-800 border border-zinc-600 text-white"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          autoComplete="email"
        />
        <label className="block text-zinc-300 text-sm mb-1">Senha</label>
        <input
          className="w-full mb-6 px-3 py-2 rounded bg-zinc-800 border border-zinc-600 text-white"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          autoComplete="current-password"
        />
        <button
          type="submit"
          disabled={loading || !configOk}
          className="w-full py-2.5 font-semibold rounded bg-[#FFF200] text-[#001623] hover:opacity-90 disabled:opacity-50"
        >
          {loading ? "Entrando…" : "Entrar"}
        </button>
      </form>
    </div>
  );
}
