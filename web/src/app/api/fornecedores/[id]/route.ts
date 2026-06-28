import { NextRequest, NextResponse } from "next/server";
import { getBackendApiBase } from "@/lib/server-api-base";

export const dynamic = "force-dynamic";

async function proxy(request: NextRequest, id: string, suffix: string, method: string) {
  let base: string;
  try {
    base = getBackendApiBase();
  } catch (e) {
    return NextResponse.json({ error: String(e) }, { status: 500 });
  }
  const auth = request.headers.get("authorization");
  if (!auth) return NextResponse.json({ error: "Sem Authorization" }, { status: 401 });

  const r = await fetch(`${base}/api/fornecedores/${encodeURIComponent(id)}${suffix}`, {
    method,
    headers: {
      Authorization: auth,
      "Content-Type": request.headers.get("content-type") ?? "application/json",
    },
    body: method === "GET" || method === "DELETE" ? undefined : await request.text(),
    cache: "no-store",
  });

  const body = await r.text();
  return new NextResponse(body, {
    status: r.status,
    headers: { "Content-Type": r.headers.get("content-type") ?? "application/json" },
  });
}

export async function GET(_request: NextRequest, ctx: { params: Promise<{ id: string }> }) {
  const { id } = await ctx.params;
  return proxy(_request, id, "", "GET");
}

export async function PUT(request: NextRequest, ctx: { params: Promise<{ id: string }> }) {
  const { id } = await ctx.params;
  return proxy(request, id, "", "PUT");
}

export async function DELETE(request: NextRequest, ctx: { params: Promise<{ id: string }> }) {
  const { id } = await ctx.params;
  return proxy(request, id, "", "DELETE");
}
