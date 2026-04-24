import { redirect } from "next/navigation";
import { createClient } from "@/lib/supabase/server";
import { AppNav } from "@/components/AppNav";
import { SignOutButton } from "@/components/SignOutButton";
import { apiGet } from "@/lib/api";

export const dynamic = "force-dynamic";

type Sync = { id: string; email: string; perfil: string; ativo?: boolean };

export default async function DashboardLayout({ children }: { children: React.ReactNode }) {
  const supabase = await createClient();
  const {
    data: { session },
  } = await supabase.auth.getSession();
  if (!session?.access_token) redirect("/login");

  let isAdmin = false;
  const api = process.env.NEXT_PUBLIC_API_URL;
  if (api) {
    try {
      const me = await apiGet<Sync>("/api/auth/sync", session.access_token);
      isAdmin = me.perfil === "Admin";
    } catch {
      isAdmin = false;
    }
  }

  return (
    <div className="min-h-screen bg-zinc-950 text-zinc-100">
      <div className="max-w-6xl mx-auto px-4 py-1 flex justify-end">
        <SignOutButton />
      </div>
      <AppNav isAdmin={isAdmin} email={session.user.email ?? undefined} />
      <div className="max-w-6xl mx-auto px-4 py-6">{children}</div>
    </div>
  );
}
