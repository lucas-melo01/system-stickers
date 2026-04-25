import { NextRequest, NextResponse } from "next/server";
import { getBackendApiBase } from "@/lib/server-api-base";

export const dynamic = "force-dynamic";

// BFF para o lote de ZPL de todas as etiquetas pendentes (consumido pelo
// botão "Imprimir todas as pendentes" via QZ Tray). Espelha o endpoint
// /api/pedido-itens/pendentes-impressao/zpl.json do backend .NET.
// Repassa q/data/ids para que a impressão respeite o filtro corrente da
// página (ou a selecção explícita do operador).
export async function GET(request: NextRequest) {
  let base: string;
  try {
    base = getBackendApiBase();
  } catch (e) {
    return NextResponse.json({ error: String(e) }, { status: 500 });
  }
  const auth = request.headers.get("authorization");
  if (!auth) {
    return NextResponse.json({ error: "Sem Authorization" }, { status: 401 });
  }

  const inSp = request.nextUrl.searchParams;
  const out = new URLSearchParams();
  for (const k of ["q", "data", "ids"]) {
    const v = inSp.get(k);
    if (v) out.set(k, v);
  }
  const qs = out.toString();
  const url = `${base}/api/pedido-itens/pendentes-impressao/zpl.json${qs ? `?${qs}` : ""}`;

  const r = await fetch(url, {
    headers: { Authorization: auth },
    cache: "no-store",
  });
  const body = await r.text();
  return new NextResponse(body, {
    status: r.status,
    headers: { "Content-Type": r.headers.get("content-type") ?? "application/json" },
  });
}
