"use client";

import { useRouter } from "next/navigation";
import { createClient } from "@/lib/supabase/client";

export function SignOutButton() {
  const router = useRouter();
  return (
    <button
      type="button"
      onClick={async () => {
        const supabase = createClient();
        await supabase.auth.signOut();
        router.replace("/login");
        router.refresh();
      }}
      className="text-zinc-500 hover:text-zinc-300 text-sm"
    >
      Sair
    </button>
  );
}
