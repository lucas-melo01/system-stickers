import Link from "next/link";

export function AppNav({
  isAdmin,
  email,
}: {
  isAdmin: boolean;
  email?: string;
}) {
  return (
    <header className="bg-[#001623] text-[#FFF200] border-b border-zinc-800">
      <div className="max-w-6xl mx-auto px-4 py-3 flex flex-wrap items-center justify-between gap-2">
        <span className="font-bold">Sistema Etiquetas</span>
        <nav className="flex flex-wrap gap-4 text-sm">
          <Link href="/pedidos" className="hover:underline">
            Pedidos
          </Link>
          <Link href="/relatorios" className="hover:underline">
            Relatórios
          </Link>
          {isAdmin && (
            <Link href="/admin/usuarios" className="hover:underline">
              Usuários
            </Link>
          )}
        </nav>
        <span className="text-zinc-400 text-sm truncate max-w-[200px]">{email}</span>
      </div>
    </header>
  );
}
