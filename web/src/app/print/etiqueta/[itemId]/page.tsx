"use client";

import { useEffect, useRef, useState } from "react";
import { useParams } from "next/navigation";
import { createClient } from "@/lib/supabase/client";
import { EtiquetaImprimir, type EtiquetaData } from "@/components/EtiquetaImprimir";

export default function PrintEtiquetaPage() {
  const params = useParams<{ itemId: string }>();
  const itemId = Number(params?.itemId);
  const [data, setData] = useState<EtiquetaData | null>(null);
  const [erro, setErro] = useState<string | null>(null);
  // Garante que só disparamos window.print() uma vez e que o marcar-impresso
  // só corre depois de o diálogo ser confirmado (afterprint).
  const impressaoIniciada = useRef(false);
  const marcado = useRef(false);

  useEffect(() => {
    async function run() {
      if (!Number.isFinite(itemId)) {
        setErro("ID inválido.");
        return;
      }
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
        const r = await fetch(`/api/pedido-itens/${itemId}/etiqueta`, {
          headers: { Authorization: `Bearer ${token}` },
          cache: "no-store",
        });
        if (!r.ok) {
          const t = await r.text();
          throw new Error(t || `HTTP ${r.status}`);
        }
        const j = (await r.json()) as EtiquetaData;
        setData(j);
      } catch (e) {
        setErro(String(e));
      }
    }
    run();
  }, [itemId]);

  // Ao terminar o diálogo (confirmado ou cancelado), marca como impresso.
  // Se o utilizador cancelar, ainda assim marcamos — senão precisaria de um
  // mecanismo adicional de detecção de cancelamento, o que não existe fiável
  // em browsers. Critério do cliente: pediu impressão, registamos como feito.
  useEffect(() => {
    async function afterPrint() {
      if (marcado.current) return;
      marcado.current = true;
      try {
        const supabase = createClient();
        const {
          data: { session },
        } = await supabase.auth.getSession();
        const token = session?.access_token;
        if (!token) return;
        await fetch(`/api/pedido-itens/${itemId}/marcar-impresso`, {
          method: "POST",
          headers: { Authorization: `Bearer ${token}` },
        });
      } finally {
        setTimeout(() => {
          try {
            window.close();
          } catch {
            /* ignora: se foi aberta noutra aba sem opener, o browser bloqueia close */
          }
        }, 150);
      }
    }
    window.addEventListener("afterprint", afterPrint);
    return () => window.removeEventListener("afterprint", afterPrint);
  }, [itemId]);

  // Quando os dados chegarem, dispara o diálogo de impressão automaticamente.
  useEffect(() => {
    if (!data || impressaoIniciada.current) return;
    impressaoIniciada.current = true;
    const t = setTimeout(() => window.print(), 300);
    return () => clearTimeout(t);
  }, [data]);

  if (erro) {
    return (
      <div className="screen-only" style={{ color: "#b00" }}>
        {erro}
      </div>
    );
  }

  if (!data) {
    return <div className="screen-only">A carregar etiqueta…</div>;
  }

  return (
    <>
      <EtiquetaImprimir data={data} />
      <div className="screen-only">
        Prévia da etiqueta. Se o diálogo não abrir sozinho, use Ctrl+P.
      </div>
    </>
  );
}
