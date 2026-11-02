/** Pedidos Nuvemshop: `ns:{storeId}:{number}` → só o number na UI. */
const NUVEMSHOP_PREFIX = /^ns:\d+:(.+)$/;

export function formatPedidoExternoId(id: string | null | undefined): string {
  if (id == null) return "";
  const trimmed = id.trim();
  if (!trimmed) return "";
  const m = NUVEMSHOP_PREFIX.exec(trimmed);
  return m?.[1] ?? trimmed;
}
