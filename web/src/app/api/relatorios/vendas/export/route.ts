import { NextRequest, NextResponse } from "next/server";
import { getBackendApiBase } from "@/lib/server-api-base";

export const dynamic = "force-dynamic";
export const maxDuration = 60;

/**
 * Proxy same-origin: o browser chama a Vercel, o servidor reenvia à API
 * (evita CORS no download do Excel e no preflight com Authorization).
 */
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

  const { searchParams } = new URL(request.url);
  const inicio = searchParams.get("inicio");
  const fim = searchParams.get("fim");
  if (!inicio || !fim) {
    return NextResponse.json({ error: "Query inicio e fim são obrigatórios" }, { status: 400 });
  }

  const url = new URL("/api/relatorios/vendas/export.xlsx", base);
  url.searchParams.set("inicio", inicio);
  url.searchParams.set("fim", fim);

  const r = await fetch(url.toString(), {
    headers: { Authorization: auth },
    cache: "no-store",
  });

  if (!r.ok) {
    const t = await r.text();
    return new NextResponse(t, { status: r.status });
  }

  const buf = await r.arrayBuffer();
  const cd = r.headers.get("content-disposition");
  const ct =
    r.headers.get("content-type") ??
    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

  const headers = new Headers();
  headers.set("Content-Type", ct);
  if (cd) headers.set("Content-Disposition", cd);

  return new NextResponse(buf, { status: 200, headers });
}
