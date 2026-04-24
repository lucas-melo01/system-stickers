"use client";

import { useState } from "react";

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
    <form onSubmit={submit} className="mb-6 p-4 border border-zinc-800 rounded-lg bg-zinc-900/30">
      <h2 className="text-sm font-semibold text-[#FFF200] mb-2">Criar usuário (Supabase Auth)</h2>
      <p className="text-xs text-zinc-500 mb-3">
        Requer <code className="text-zinc-400">SUPABASE_SERVICE_ROLE_KEY</code> no servidor (Vercel) e e-mail
        do administrador em <code className="text-zinc-400">ADMIN_INVITE_EMAILS</code>.
      </p>
      <div className="grid gap-2 sm:grid-cols-2 sm:items-end">
        <div>
          <label className="text-xs text-zinc-500">E-mail</label>
          <input
            className="w-full px-2 py-1 rounded bg-zinc-800 border border-zinc-600 text-sm"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            type="email"
            required
          />
        </div>
        <div>
          <label className="text-xs text-zinc-500">Senha inicial</label>
          <input
            className="w-full px-2 py-1 rounded bg-zinc-800 border border-zinc-600 text-sm"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            type="password"
            minLength={6}
            required
          />
        </div>
        <button
          type="submit"
          disabled={loading}
          className="sm:col-span-2 px-3 py-1.5 bg-[#001623] text-[#FFF200] text-sm font-semibold rounded border border-zinc-600 w-fit"
        >
          {loading ? "Criando…" : "Criar"}
        </button>
      </div>
      {err && <p className="text-red-400 text-sm mt-2">{err}</p>}
    </form>
  );
}
