"use client";

import { useRouter } from "next/navigation";
import { createClient } from "@/lib/supabase/client";
import Button from "@mui/material/Button";

export function SignOutButton() {
  const router = useRouter();
  return (
    <Button
      type="button"
      variant="outlined"
      size="small"
      color="primary"
      onClick={async () => {
        const supabase = createClient();
        await supabase.auth.signOut();
        router.replace("/login");
        router.refresh();
      }}
    >
      Sair
    </Button>
  );
}
