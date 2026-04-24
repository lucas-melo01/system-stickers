import { isWebSerialSupported, printZplOverUsb } from "@/lib/print-zpl-serial";

/** Obtém ZPL, imprime N vias, marca o item como impresso (uma chamada de marcar após tudo). */
export async function printItemLabelsBatched(
  itemId: number,
  accessToken: string,
  baudRate: number,
  copies: number
): Promise<void> {
  const c = Math.max(1, Math.min(copies || 1, 50));
  const r = await fetch(`/api/pedido-itens/${itemId}/zpl-json`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  if (!r.ok) throw new Error((await r.text()) || "Falha ao obter ZPL");
  const j = (await r.json()) as { zpl: string };
  if (!j.zpl?.length) throw new Error("ZPL vazio");
  if (!isWebSerialSupported()) {
    throw new Error("Web Serial indisponível. Use Chrome ou Edge em HTTPS.");
  }
  for (let i = 0; i < c; i++) {
    await printZplOverUsb(j.zpl, baudRate);
  }
  const m = await fetch(`/api/pedido-itens/${itemId}/marcar-impresso`, {
    method: "POST",
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  if (!m.ok) throw new Error((await m.text()) || "Falha ao marcar impresso");
}
