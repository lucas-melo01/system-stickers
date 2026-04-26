import { NextRequest, NextResponse } from "next/server";
import { getBackendApiBase } from "@/lib/server-api-base";

export const dynamic = "force-dynamic";

/**
 * BFF: GET /api/admin/usuarios — repassa o JWT do utilizador actual para
 * a API .NET (mesmo padrão do PATCH e do provision).
 */
export async function GET(request: NextRequest) {
  let base: string;
  try {
    base = getBackendApiBase();
  } catch (e) {
    return NextResponse.json({ error: String(e) }, { status: 500 });
  }
  const auth = request.headers.get("authorization");
  if (!auth) return NextResponse.json({ error: "Sem Authorization" }, { status: 401 });
  const r = await fetch(`${base}/api/admin/usuarios`, {
    method: "GET",
    headers: { Authorization: auth, Accept: "application/json" },
    cache: "no-store",
  });
  const body = await r.text();
  return new NextResponse(body, {
    status: r.status,
    headers: { "Content-Type": r.headers.get("content-type") ?? "application/json" },
  });
}
