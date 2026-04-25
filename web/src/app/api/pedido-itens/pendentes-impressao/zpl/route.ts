import { NextRequest, NextResponse } from "next/server";
import { getBackendApiBase } from "@/lib/server-api-base";

export const dynamic = "force-dynamic";

// BFF para o lote de ZPL de todas as etiquetas pendentes (consumido pelo
// botão "Imprimir todas as pendentes" via QZ Tray). Espelha o endpoint
// /api/pedido-itens/pendentes-impressao/zpl.json do backend .NET.
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
  const url = `${base}/api/pedido-itens/pendentes-impressao/zpl.json`;
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
