import { redirect } from "next/navigation";
import { createClient } from "@/lib/supabase/server";
import { fetchPerfilAtual } from "@/lib/auth-sync";
import { isAdminPerfil } from "@/lib/is-admin-perfil";
import { DashboardFrame } from "@/components/DashboardFrame";

export const dynamic = "force-dynamic";

export default async function DashboardLayout({ children }: { children: React.ReactNode }) {
  const supabase = await createClient();
  if (!supabase) redirect("/login");
  const {
    data: { session },
  } = await supabase.auth.getSession();
  if (!session?.access_token) redirect("/login");

  const me = await fetchPerfilAtual(session.access_token);
  const isAdmin = isAdminPerfil(me?.perfil);

  return (
    <DashboardFrame email={session.user.email ?? undefined} isAdmin={isAdmin}>
      {children}
    </DashboardFrame>
  );
}
