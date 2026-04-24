export type EtiquetaData = {
  itemId: number;
  pedidoId: number;
  nomeCliente: string;
  cpfCliente: string;
  data: string;
  modelo: string;
  cor: string;
  tamanho: string;
  codFornecedor: string;
};

/**
 * Renderiza uma etiqueta 60x40 mm no layout acordado:
 *  - Linha 1: Nome Cliente (esq) | Data (dir)
 *  - Linha 2: Modelo | Cor | Tamanho
 *  - Linha 3: CPF (esq) | Cod. Fornecedor (dir)
 */
export function EtiquetaImprimir({ data }: { data: EtiquetaData }) {
  return (
    <div className="etiqueta" data-item-id={data.itemId}>
      <div className="row">
        <div className="field">
          <span className="label">Nome Cliente</span>
          <span className="value big">{data.nomeCliente || "—"}</span>
        </div>
        <div className="field right">
          <span className="label">Data</span>
          <span className="value">{data.data || "—"}</span>
        </div>
      </div>

      <div className="row middle">
        <div className="field">
          <span className="label">Modelo Peça</span>
          <span className="value">{data.modelo || "—"}</span>
        </div>
        <div className="field center">
          <span className="label">Cor</span>
          <span className="value">{data.cor || "—"}</span>
        </div>
        <div className="field right">
          <span className="label">Tamanho</span>
          <span className="value">{data.tamanho || "—"}</span>
        </div>
      </div>

      <div className="row">
        <div className="field">
          <span className="label">CPF Cliente</span>
          <span className="value small">{data.cpfCliente || "—"}</span>
        </div>
        <div className="field right">
          <span className="label">Cod. Fornecedor</span>
          <span className="value small">{data.codFornecedor || "—"}</span>
        </div>
      </div>
    </div>
  );
}
