/**
 * Cliente do QZ Tray (modo Community, sem certificado de assinatura).
 *
 * Modelo:
 *  - Lazy-load do pacote `qz-tray` (depende de `window`/`WebSocket`, então
 *    nunca pode ser importado em código que corra no servidor).
 *  - Conexão única (singleton) e reutilizada entre chamadas.
 *  - Sem assinatura: a 1.ª impressão por sessão mostra o popup nativo do QZ
 *    a perguntar "Allow this site to access printers?". Se o utilizador
 *    marcar "Remember decision", nunca mais vê o popup.
 *  - Persistência da impressora preferida em localStorage para não obrigar
 *    o operador a escolher a cada impressão.
 */

const STORAGE_KEY_PRINTER = "qz.printerName";

type QzApiModule = typeof import("qz-tray").default;

let qzPromise: Promise<QzApiModule> | null = null;
let connectPromise: Promise<void> | null = null;

/**
 * Erro tipado para distinguir falhas de conexão (cliente sem QZ instalado /
 * a correr) de outras falhas (impressora inexistente, payload inválido, etc.).
 */
export class QzNotAvailableError extends Error {
  constructor(message = "QZ Tray não está em execução nesta máquina.") {
    super(message);
    this.name = "QzNotAvailableError";
  }
}

async function loadQz(): Promise<QzApiModule> {
  if (typeof window === "undefined") {
    throw new Error("qz-print só pode ser usado no browser.");
  }
  if (!qzPromise) {
    qzPromise = import("qz-tray").then((mod) => {
      const qz = (mod.default ?? mod) as QzApiModule;
      // Em modo Community deixamos a security como veio: o QZ pede confirmação
      // ao utilizador na 1.ª impressão. Se mais tarde adquirirmos certificado,
      // basta configurar aqui qz.security.setCertificatePromise / SignaturePromise.
      return qz;
    });
  }
  return qzPromise;
}

/**
 * Garante uma conexão WebSocket activa com o QZ Tray local. Reaproveita a
 * existente se já estiver ligada. Em caso de falha (QZ desinstalado, parado
 * ou bloqueado por antivírus), lança QzNotAvailableError.
 */
export async function ensureQzConnected(): Promise<QzApiModule> {
  const qz = await loadQz();
  if (qz.websocket.isActive()) return qz;
  if (!connectPromise) {
    connectPromise = qz.websocket
      .connect({
        // Defaults do QZ: wss://localhost:{8181..8184} ou ws://localhost:8182.
        // Mantemos os defaults para máxima compatibilidade.
        retries: 1,
        delay: 1,
      })
      .catch((e: unknown) => {
        connectPromise = null;
        throw new QzNotAvailableError(
          "Não foi possível ligar ao QZ Tray. Verifique se está instalado e a correr no tabuleiro do sistema. Detalhe: " +
            String(e instanceof Error ? e.message : e)
        );
      });
  }
  await connectPromise;
  return qz;
}

/** Tenta uma conexão rápida; devolve true/false sem lançar. */
export async function isQzAvailable(): Promise<boolean> {
  try {
    await ensureQzConnected();
    return true;
  } catch {
    return false;
  }
}

/** Lista todas as impressoras visíveis pelo QZ (= as que o Windows vê). */
export async function listPrinters(): Promise<string[]> {
  const qz = await ensureQzConnected();
  const r = await qz.printers.find();
  if (Array.isArray(r)) return r;
  return r ? [r] : [];
}

/** Nome da impressora padrão configurada no Windows. */
export async function getDefaultPrinter(): Promise<string> {
  const qz = await ensureQzConnected();
  return qz.printers.getDefault();
}

/** Lê do localStorage a impressora escolhida pelo operador (ou null). */
export function getPreferredPrinter(): string | null {
  if (typeof window === "undefined") return null;
  return window.localStorage.getItem(STORAGE_KEY_PRINTER);
}

/** Persiste no localStorage a impressora escolhida pelo operador. */
export function setPreferredPrinter(name: string | null): void {
  if (typeof window === "undefined") return;
  if (!name) {
    window.localStorage.removeItem(STORAGE_KEY_PRINTER);
  } else {
    window.localStorage.setItem(STORAGE_KEY_PRINTER, name);
  }
}

/**
 * Devolve o nome da impressora a usar: 1) a preferida pelo operador, se ainda
 * existir no Windows; 2) caso contrário, a padrão do Windows. Lança se nenhuma
 * impressora estiver instalada.
 */
export async function resolvePrinter(): Promise<string> {
  const preferred = getPreferredPrinter();
  if (preferred) {
    const all = await listPrinters();
    if (all.includes(preferred)) return preferred;
  }
  const def = await getDefaultPrinter();
  if (!def) throw new Error("Nenhuma impressora configurada no Windows.");
  return def;
}

/** Envia uma única etiqueta ZPL para a impressora indicada. */
export async function printZpl(printerName: string, zpl: string): Promise<void> {
  if (!zpl?.trim()) throw new Error("ZPL vazio.");
  const qz = await ensureQzConnected();
  const config = qz.configs.create(printerName);
  await qz.print(config, [
    {
      type: "raw",
      format: "command",
      flavor: "plain",
      data: zpl,
    },
  ]);
}

/**
 * Envia várias etiquetas ZPL na mesma chamada. O QZ envia tudo numa única
 * operação ao spooler, evitando race conditions e mantendo a ordem.
 */
export async function printZplBatch(printerName: string, zpls: string[]): Promise<void> {
  const valid = zpls.filter((z) => z?.trim());
  if (valid.length === 0) return;
  const qz = await ensureQzConnected();
  const config = qz.configs.create(printerName);
  await qz.print(
    config,
    valid.map((zpl) => ({
      type: "raw",
      format: "command",
      flavor: "plain",
      data: zpl,
    }))
  );
}
