"use client";

import { useState } from "react";
import { createClient } from "@/lib/supabase/client";
import { apiPatch } from "@/lib/api";
import { InviteForm } from "./invite-form";

type U = { id: string; email: string; nome: string | null; perfil: string; ativo: boolean; criadoEm: string };

export function UsuariosClient({ initial }: { initial: U[] }) {
  const [list, setList] = useState(initial);
  const [msg, setMsg] = useState<string | null>(null);

  async function patch(id: string, body: { perfil?: string; ativo?: boolean }) {
    setMsg(null);
    try {
      const supabase = createClient();
      const { data: { session } } = await supabase.auth.getSession();
      if (!session?.access_token) throw new Error("Sessão");
      const u = await apiPatch<U>(`/api/admin/usuarios/${id}`, session.access_token, body);
      setList((prev) => prev.map((x) => (x.id === id ? { ...x, ...u } : x)));
      setMsg("Salvo");
    } catch (e) {
      setMsg(String(e));
    }
  }

  return (
    <div>
      <InviteForm
        onCreated={() => {
          window.location.reload();
        }}
      />
      {msg && <p className="text-sm text-zinc-400 mb-2">{msg}</p>}
      <div className="overflow-x-auto border border-zinc-800 rounded-lg">
        <table className="w-full text-sm">
          <thead>
            <tr className="bg-zinc-900 text-left text-zinc-400">
              <th className="p-2">E-mail</th>
              <th className="p-2">Perfil</th>
              <th className="p-2">Ativo</th>
              <th className="p-2">Ações</th>
            </tr>
          </thead>
          <tbody>
            {list.map((r) => (
              <tr key={r.id} className="border-t border-zinc-800">
                <td className="p-2">{r.email}</td>
                <td className="p-2">{r.perfil}</td>
                <td className="p-2">{r.ativo ? "Sim" : "Não"}</td>
                <td className="p-2">
                  <div className="flex flex-wrap gap-1">
                    {r.perfil === "Operador" ? (
                      <button
                        type="button"
                        className="text-xs px-2 py-0.5 rounded bg-zinc-800"
                        onClick={() => patch(r.id, { perfil: "Admin" })}
                      >
                        Tornar admin
                      </button>
                    ) : (
                      <button
                        type="button"
                        className="text-xs px-2 py-0.5 rounded bg-zinc-800"
                        onClick={() => patch(r.id, { perfil: "Operador" })}
                      >
                        Tornar operador
                      </button>
                    )}
                    <button
                      type="button"
                      className="text-xs px-2 py-0.5 rounded bg-zinc-800"
                      onClick={() => patch(r.id, { ativo: !r.ativo })}
                    >
                      {r.ativo ? "Desativar" : "Ativar"}
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
