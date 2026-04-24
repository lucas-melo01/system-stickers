import { NextRequest, NextResponse } from "next/server";
import { getBackendApiBase } from "@/lib/server-api-base";

export const dynamic = "force-dynamic";

/**
 * BFF: PATCH /api/admin/usuarios/:id — evita CORS cross-origin entre browser e Render,
 * repassando para a API com o JWT do Supabase do utilizador actual.
 */
export async function PATCH(request: NextRequest, ctx: { params: Promise<{ id: string }> }) {
  let base: string;
  try {
    base = getBackendApiBase();
  } catch (e) {
    return NextResponse.json({ error: String(e) }, { status: 500 });
  }
  const auth = request.headers.get("authorization");
  if (!auth) return NextResponse.json({ error: "Sem Authorization" }, { status: 401 });

  const { id } = await ctx.params;
  const payload = await request.text();

  const r = await fetch(`${base}/api/admin/usuarios/${encodeURIComponent(id)}`, {
    method: "PATCH",
    headers: {
      Authorization: auth,
      "Content-Type": "application/json",
    },
    body: payload,
    cache: "no-store",
  });

  const body = await r.text();
  return new NextResponse(body, {
    status: r.status,
    headers: { "Content-Type": r.headers.get("content-type") ?? "application/json" },
  });
}
