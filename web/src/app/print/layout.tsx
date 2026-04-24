import type { Metadata } from "next";
import "./print.css";

export const metadata: Metadata = {
  title: "Impressão de etiqueta",
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
