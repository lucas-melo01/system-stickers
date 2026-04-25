// Shim mínimo de tipos para o pacote qz-tray (não traz typings oficiais).
// Cobre apenas o subset que usamos em lib/qz-print.ts. Se precisarmos de mais
// API no futuro, basta adicionar aqui.

declare module "qz-tray" {
  type SignaturePromise = (toSign: string) => Promise<string>;

  interface PrinterConfig {
    setPrinter(name: string): void;
  }

  interface PrintData {
    type: "raw" | "pixel";
    format?: "command" | "image" | "pdf" | "html";
    flavor?: "plain" | "base64" | "file";
    data: string;
  }

  interface QzApi {
    websocket: {
      connect(opts?: {
        host?: string | string[];
        port?: { secure?: number[]; insecure?: number[] };
        usingSecure?: boolean;
        retries?: number;
        delay?: number;
      }): Promise<void>;
      disconnect(): Promise<void>;
      isActive(): boolean;
    };
    printers: {
      find(query?: string): Promise<string | string[]>;
      getDefault(): Promise<string>;
    };
    configs: {
      create(printer: string, opts?: Record<string, unknown>): PrinterConfig;
    };
    print(config: PrinterConfig, data: Array<PrintData | string>): Promise<void>;
    security: {
      setCertificatePromise(cb: (resolve: (cert: string) => void, reject: (err: Error) => void) => void): void;
      setSignaturePromise(cb: (toSign: string) => SignaturePromise): void;
      setSignatureAlgorithm(alg: string): void;
    };
    api: {
      setSha256Type(cb: (data: string) => string | Promise<string>): void;
      setPromiseType(cb: <T>(resolver: (resolve: (value: T) => void, reject: (err: unknown) => void) => void) => Promise<T>): void;
    };
  }

  const qz: QzApi;
  export default qz;
}
