"use client";

import { useEffect, useRef, useState } from "react";
import { createClient } from "@/lib/supabase/client";
import { EtiquetaImprimir, type EtiquetaData } from "@/components/EtiquetaImprimir";

export default function PrintPendentesPage() {
  const [lista, setLista] = useState<EtiquetaData[] | null>(null);
  const [erro, setErro] = useState<string | null>(null);
  const impressaoIniciada = useRef(false);
  const marcado = useRef(false);

  useEffect(() => {
    async function run() {
      try {
        const supabase = createClient();
        const {
          data: { session },
        } = await supabase.auth.getSession();
        const token = session?.access_token;
        if (!token) {
          setErro("Sessão expirou. Feche esta janela e entre de novo.");
          return;
        }
        const r = await fetch(`/api/pedido-itens/pendentes-impressao/etiquetas`, {
          headers: { Authorization: `Bearer ${token}` },
          cache: "no-store",
        });
        if (!r.ok) {
          const t = await r.text();
          throw new Error(t || `HTTP ${r.status}`);
        }
        const j = (await r.json()) as EtiquetaData[];
        setLista(j);
      } catch (e) {
        setErro(String(e));
      }
    }
    run();
  }, []);

  // Depois do diálogo, marca todas como impressas em paralelo.
  useEffect(() => {
    async function afterPrint() {
      if (marcado.current || !lista || lista.length === 0) return;
      marcado.current = true;
      try {
        const supabase = createClient();
        const {
          data: { session },
        } = await supabase.auth.getSession();
        const token = session?.access_token;
        if (!token) return;
        await Promise.allSettled(
          lista.map((it) =>
            fetch(`/api/pedido-itens/${it.itemId}/marcar-impresso`, {
              method: "POST",
              headers: { Authorization: `Bearer ${token}` },
            })
          )
        );
      } finally {
        setTimeout(() => {
          try {
            window.close();
          } catch {
            /* ignora */
          }
        }, 150);
      }
    }
    window.addEventListener("afterprint", afterPrint);
    return () => window.removeEventListener("afterprint", afterPrint);
  }, [lista]);

  useEffect(() => {
    if (!lista || lista.length === 0 || impressaoIniciada.current) return;
    impressaoIniciada.current = true;
    const t = setTimeout(() => window.print(), 400);
    return () => clearTimeout(t);
  }, [lista]);

  if (erro) {
    return (
      <div className="screen-only" style={{ color: "#b00" }}>
        {erro}
      </div>
    );
  }

  if (!lista) {
    return <div className="screen-only">A carregar etiquetas pendentes…</div>;
  }

  if (lista.length === 0) {
    return <div className="screen-only">Não há etiquetas pendentes.</div>;
  }

  return (
    <>
      {lista.map((e) => (
        <EtiquetaImprimir key={e.itemId} data={e} />
      ))}
      <div className="screen-only">
        {lista.length} etiqueta(s) preparada(s). Se o diálogo não abrir sozinho, use Ctrl+P.
      </div>
    </>
  );
}
