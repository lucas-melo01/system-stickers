import { NextRequest, NextResponse } from "next/server";
import { getBackendApiBase } from "@/lib/server-api-base";

export const dynamic = "force-dynamic";

async function proxy(request: NextRequest, path: string, method: string) {
  let base: string;
  try {
    base = getBackendApiBase();
  } catch (e) {
    return NextResponse.json({ error: String(e) }, { status: 500 });
  }
  const auth = request.headers.get("authorization");
  if (!auth) return NextResponse.json({ error: "Sem Authorization" }, { status: 401 });

  const url = new URL(request.url);
  const qs = url.searchParams.toString();
  const target = `${base}/api/fornecedores${path}${qs ? `?${qs}` : ""}`;

  const r = await fetch(target, {
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

export async function GET(request: NextRequest) {
  return proxy(request, "", "GET");
}

export async function POST(request: NextRequest) {
  return proxy(request, "", "POST");
}
