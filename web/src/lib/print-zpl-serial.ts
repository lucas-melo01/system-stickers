/**
 * Envia ZPL bruto para impressora térmica via Web Serial (Chrome/Edge, HTTPS).
 * A impressora USB costuma aparecer como porta COM — o browser pede permissão na 1.ª vez.
 */

type SerialPortLike = {
  open: (options: {
    baudRate: number;
    dataBits?: number;
    stopBits?: number;
    parity?: "none" | "even" | "odd";
    flowControl?: "none" | "hardware";
  }) => Promise<void>;
  close: () => Promise<void>;
  writable: WritableStream<Uint8Array> | null;
};

type SerialNavigator = Navigator & {
  serial: {
    getPorts: () => Promise<SerialPortLike[]>;
    requestPort: () => Promise<SerialPortLike>;
  };
};

export function isWebSerialSupported(): boolean {
  return typeof navigator !== "undefined" && "serial" in navigator;
}

export async function printZplOverUsb(zpl: string, baudRate: number): Promise<void> {
  if (!isWebSerialSupported()) {
    throw new Error("Web Serial não disponível. Use Chrome ou Edge em HTTPS.");
  }
  const { serial } = navigator as SerialNavigator;
  const granted = await serial.getPorts();
  const port = granted[0] ?? (await serial.requestPort());
  await port.open({
    baudRate,
    dataBits: 8,
    stopBits: 1,
    parity: "none",
    flowControl: "none",
  });
  const enc = new TextEncoder();
  const bytes = enc.encode(zpl.endsWith("\n") ? zpl : `${zpl}\n`);
  const writer = port.writable?.getWriter();
  if (!writer) {
    await port.close();
    throw new Error("Porta serial sem escrita.");
  }
  try {
    await writer.write(bytes);
  } finally {
    writer.releaseLock();
  }
  await port.close();
}
