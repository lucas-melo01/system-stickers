import { NextRequest, NextResponse } from "next/server";
import { getBackendApiBase } from "@/lib/server-api-base";

export const dynamic = "force-dynamic";

export async function GET(_request: NextRequest, ctx: { params: Promise<{ id: string }> }) {
  let base: string;
  try {
    base = getBackendApiBase();
  } catch (e) {
    return NextResponse.json({ error: String(e) }, { status: 500 });
  }
  const auth = _request.headers.get("authorization");
  if (!auth) return NextResponse.json({ error: "Sem Authorization" }, { status: 401 });

  const { id } = await ctx.params;
  const r = await fetch(`${base}/api/notificacoes/pedido-compra/${encodeURIComponent(id)}`, {
    headers: { Authorization: auth },
    cache: "no-store",
  });

  const body = await r.text();
  return new NextResponse(body, {
    status: r.status,
    headers: { "Content-Type": r.headers.get("content-type") ?? "application/json" },
  });
}
