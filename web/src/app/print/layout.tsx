import type { Metadata } from "next";
import "./print.css";

// Título em branco para não aparecer como cabeçalho quando o Chrome imprime
// com "Cabeçalhos e rodapés" activos. O cliente continua a dever desmarcar essa
// opção no 1.º uso, mas isto já remove a string "Impressão de etiqueta".
export const metadata: Metadata = {
  title: { absolute: " " },
};

// Layout próprio para as páginas de impressão: sem sidebar, sem AppBar, sem MUI.
// O objectivo é dar ao browser o mínimo de "ruído" ao gerar o documento para o spooler.
export default function PrintLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return <div className="print-root">{children}</div>;
}
