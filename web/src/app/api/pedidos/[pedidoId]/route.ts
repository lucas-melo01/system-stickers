import { NextRequest, NextResponse } from "next/server";
import { getBackendApiBase } from "@/lib/server-api-base";

export const dynamic = "force-dynamic";

export async function PUT(
  request: NextRequest,
  context: { params: Promise<{ pedidoId: string }> }
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

  const { pedidoId } = await context.params;
  const payload = await request.text();
  const r = await fetch(`${base}/api/pedidos/${pedidoId}`, {
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

export async function DELETE(
  request: NextRequest,
  context: { params: Promise<{ pedidoId: string }> }
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

  const { pedidoId } = await context.params;
  const r = await fetch(`${base}/api/pedidos/${pedidoId}`, {
    method: "DELETE",
    headers: { Authorization: auth },
  });
  if (r.status === 204) return new NextResponse(null, { status: 204 });
  const body = await r.text();
  return new NextResponse(body || null, {
    status: r.status,
    headers: { "Content-Type": r.headers.get("content-type") ?? "application/json" },
  });
}
