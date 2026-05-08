import { NextRequest, NextResponse } from "next/server";
import { getBackendApiBase } from "@/lib/server-api-base";

export const dynamic = "force-dynamic";

export async function PUT(
  request: NextRequest,
  context: { params: Promise<{ pedidoId: string; itemId: string }> }
) {
  let base: string;
  try {
    base = getBackendApiBase();
  } catch (e) {
    return NextResponse.json({ error: String(e) }, { status: 500 });
  }
  const auth = request.headers.get("authorization");
  if (!auth)
    return NextResponse.json({ error: "Sem Authorization" }, { status: 401 });

  const { pedidoId, itemId } = await context.params;
  const payload = await request.text();
  const r = await fetch(`${base}/api/pedidos/${pedidoId}/itens/${itemId}`, {
    method: "PUT",
    headers: {
      Authorization: auth,
      "Content-Type": "application/json",
    },
    body: payload,
  });
  const body = await r.text();
  return new NextResponse(body || null, {
    status: r.status,
    headers: { "Content-Type": r.headers.get("content-type") ?? "application/json" },
  });
}
