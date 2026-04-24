"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { createClient } from "@/lib/supabase/client";
import { isWebSerialSupported, printZplOverUsb } from "@/lib/print-zpl-serial";

const BAUD_STORAGE = "zpl-serial-baud";

export function PedidoRowActions({
  itemId,
  accessToken: tokenProp,
  quantidade,
}: {
  itemId: number;
  accessToken: string;
  quantidade: number;
}) {
  const router = useRouter();
  const [msg, setMsg] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [baud, setBaud] = useState(() => {
    if (typeof window === "undefined") return 9600;
    const s = window.localStorage.getItem(BAUD_STORAGE);
    const n = s ? parseInt(s, 10) : 9600;
    return [9600, 115200, 57600, 38400, 19200].includes(n) ? n : 9600;
  });

  function persistBaud(n: number) {
    setBaud(n);
    try {
      window.localStorage.setItem(BAUD_STORAGE, String(n));
    } catch {
      /* ignore */
    }
  }

  async function getToken() {
    const supabase = createClient();
    const {
      data: { session },
    } = await supabase.auth.getSession();
    return session?.access_token ?? tokenProp;
  }

  async function marcar(t: string) {
    const r = await fetch(`/api/pedido-itens/${itemId}/marcar-impresso`, {
      method: "POST",
      headers: { Authorization: `Bearer ${t}` },
    });
    if (!r.ok) throw new Error(await r.text());
  }

  async function imprimir() {
    setMsg(null);
    setLoading(true);
    const copies = Math.max(1, Math.min(quantidade || 1, 50));
    try {
      if (!isWebSerialSupported()) {
        setMsg(
          "Navegador sem Web Serial. Use Google Chrome ou Microsoft Edge, em HTTPS, e permita a porta USB (COM) da impressora."
        );
        return;
      }
      const t = await getToken();
      if (!t) {
        setMsg("Sessão expirou. Entre de novo.");
        return;
      }
      const r = await fetch(`/api/pedido-itens/${itemId}/zpl-json`, {
        headers: { Authorization: `Bearer ${t}` },
      });
      if (!r.ok) throw new Error((await r.text()) || "Falha ao obter ZPL");
      const j = (await r.json()) as { zpl: string };
      if (!j.zpl?.length) throw new Error("ZPL vazio");

      for (let i = 0; i < copies; i++) {
        await printZplOverUsb(j.zpl, baud);
      }

      await marcar(t);
      setMsg("Impresso e atualizado");
      router.refresh();
    } catch (e) {
      setMsg(String(e));
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="flex flex-col gap-1 min-w-[140px]">
      <div className="flex flex-wrap items-center gap-1">
        <label className="text-[10px] text-zinc-500 sr-only">Baud</label>
        <select
          value={baud}
          onChange={(e) => persistBaud(Number(e.target.value))}
          className="text-[10px] rounded bg-zinc-900 border border-zinc-600 text-zinc-300 px-1 py-0.5 max-w-[5.5rem]"
          title="Velocidade da porta (se a etiqueta sair errada, tente 115200)"
        >
          <option value={9600}>9600</option>
          <option value={19200}>19200</option>
          <option value={38400}>38400</option>
          <option value={57600}>57600</option>
          <option value={115200}>115200</option>
        </select>
        <button
          type="button"
          disabled={loading}
          onClick={imprimir}
          className="text-xs px-2 py-0.5 rounded bg-[#001623] text-[#FFF200] border border-zinc-600 font-semibold"
        >
          {loading ? "…" : "Imprimir"}
        </button>
      </div>
      {msg && <span className="text-xs text-zinc-500 leading-tight">{msg}</span>}
    </div>
  );
}
