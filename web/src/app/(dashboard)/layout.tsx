import { redirect } from "next/navigation";
import { createClient } from "@/lib/supabase/server";
import { DashboardFrame } from "@/components/DashboardFrame";
import { apiGet } from "@/lib/api";
import { isAdminPerfil } from "@/lib/is-admin-perfil";

export const dynamic = "force-dynamic";

type Sync = { id: string; email: string; perfil: string | number; ativo?: boolean };

export default async function DashboardLayout({ children }: { children: React.ReactNode }) {
  const supabase = await createClient();
  if (!supabase) redirect("/login");
  const {
    data: { session },
  } = await supabase.auth.getSession();
  if (!session?.access_token) redirect("/login");

  let isAdmin = false;
  const api = process.env.NEXT_PUBLIC_API_URL;
  if (api) {
    try {
      const me = await apiGet<Sync>("/api/auth/sync", session.access_token);
      isAdmin = isAdminPerfil(me.perfil);
    } catch {
      isAdmin = false;
    }
  }

  return (
    <DashboardFrame isAdmin={isAdmin} email={session.user.email ?? undefined}>
      {children}
    </DashboardFrame>
  );
}
