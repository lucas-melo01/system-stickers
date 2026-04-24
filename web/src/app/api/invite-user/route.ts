import { createClient } from "@/lib/supabase/server";
import { createClient as createServiceClient } from "@supabase/supabase-js";
import { NextResponse } from "next/server";
import { getBackendApiBase } from "@/lib/server-api-base";
import { isAdminPerfil } from "@/lib/is-admin-perfil";
import { fetchPerfilAtual } from "@/lib/auth-sync";

export async function POST(request: Request) {
  const supabase = await createClient();
  if (!supabase) {
    return NextResponse.json(
      { error: "Supabase não configurado (NEXT_PUBLIC_SUPABASE_URL / ANON_KEY no Vercel)" },
      { status: 503 }
    );
  }
  const {
    data: { session },
  } = await supabase.auth.getSession();
  if (!session?.access_token) {
    return NextResponse.json({ error: "Não autenticado" }, { status: 401 });
  }
  const {
    data: { user },
  } = await supabase.auth.getUser();
  if (!user?.email) {
    return NextResponse.json({ error: "Não autenticado" }, { status: 401 });
  }

  const allowedEmails = (process.env.ADMIN_INVITE_EMAILS ?? "")
    .split(",")
    .map((e) => e.trim().toLowerCase())
    .filter(Boolean);

  const me = await fetchPerfilAtual(session.access_token);
  const isAppAdmin = isAdminPerfil(me?.perfil);
  if (!isAppAdmin && (allowedEmails.length === 0 || !allowedEmails.includes(user.email.toLowerCase()))) {
    return NextResponse.json({ error: "Sem permissão para convidar" }, { status: 403 });
  }

  const serviceKey = process.env.SUPABASE_SERVICE_ROLE_KEY;
  const url = process.env.NEXT_PUBLIC_SUPABASE_URL;
  if (!serviceKey || !url) {
    return NextResponse.json({ error: "Service role ou URL não configurados no servidor" }, { status: 500 });
  }

  const body = (await request.json()) as {
    email?: string;
    password?: string;
    perfil?: string | number;
  };
  const { email, password, perfil = "Operador" } = body;
  if (!email || !password) {
    return NextResponse.json({ error: "email e password obrigatórios" }, { status: 400 });
  }

  const admin = createServiceClient(url, serviceKey, {
    auth: { autoRefreshToken: false, persistSession: false },
  });
  const { data, error } = await admin.auth.admin.createUser({
    email: email.trim(),
    password,
    email_confirm: true,
  });
  if (error) {
    return NextResponse.json({ error: error.message }, { status: 400 });
  }
  const newId = data.user!.id;
  const newEmail = data.user!.email ?? email.trim();
  const perfilStr = isAdminPerfil(perfil) ? "Admin" : "Operador";

  let provisioned = false;
  try {
    const base = getBackendApiBase();
    const prov = await fetch(`${base}/api/admin/usuarios/provision`, {
      method: "POST",
      headers: {
        Authorization: `Bearer ${session.access_token}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        id: newId,
        email: newEmail,
        perfil: perfilStr,
      }),
    });
    provisioned = prov.ok;
  } catch {
    /* NEXT_PUBLIC_API_URL em falta ou API indisponível – perfil fica para o 1.º /api/auth/sync */
  }

  return NextResponse.json({ id: newId, email: newEmail, perfil: perfilStr, provisioned });
}
