export type Paged<T> = {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
};

export function normalizePaged<T>(
  raw: unknown,
  mapItem: (o: Record<string, unknown>) => T
): Paged<T> {
  const o = (raw ?? {}) as Record<string, unknown>;
  const itemsRaw = (o.items ?? o.Items ?? []) as unknown[];
  return {
    items: itemsRaw.map((x) => mapItem(x as Record<string, unknown>)),
    totalCount: Number(o.totalCount ?? o.TotalCount ?? 0),
    page: Number(o.page ?? o.Page ?? 1),
    pageSize: Number(o.pageSize ?? o.PageSize ?? 50),
    totalPages: Number(o.totalPages ?? o.TotalPages ?? 0),
  };
}

export type FornecedorLista = {
  id: number;
  nomeRazaoSocial: string;
  email: string | null;
  whatsApp: string;
  ativo: boolean;
  produtosVinculados: number;
  criadoEm: string;
  atualizadoEm: string;
};

export function normalizeFornecedor(o: Record<string, unknown>): FornecedorLista {
  return {
    id: Number(o.id ?? o.Id ?? 0),
    nomeRazaoSocial: String(o.nomeRazaoSocial ?? o.NomeRazaoSocial ?? ""),
    email: (o.email ?? o.Email ?? null) as string | null,
    whatsApp: String(o.whatsApp ?? o.WhatsApp ?? ""),
    ativo: Boolean(o.ativo ?? o.Ativo ?? true),
    produtosVinculados: Number(o.produtosVinculados ?? o.ProdutosVinculados ?? 0),
    criadoEm: String(o.criadoEm ?? o.CriadoEm ?? ""),
    atualizadoEm: String(o.atualizadoEm ?? o.AtualizadoEm ?? ""),
  };
}

export type ProdutoLista = {
  id: number;
  loja: string;
  produtoIdLojaIntegrada: number;
  nome: string;
  sku: string | null;
  codigoFornecedor: string | null;
  fornecedorId: number | null;
  fornecedorNome: string | null;
  ativo: boolean;
};

export function normalizeProduto(o: Record<string, unknown>): ProdutoLista {
  return {
    id: Number(o.id ?? o.Id ?? 0),
    loja: String(o.loja ?? o.Loja ?? ""),
    produtoIdLojaIntegrada: Number(o.produtoIdLojaIntegrada ?? o.ProdutoIdLojaIntegrada ?? 0),
    nome: String(o.nome ?? o.Nome ?? ""),
    sku: (o.sku ?? o.Sku ?? null) as string | null,
    codigoFornecedor: (o.codigoFornecedor ?? o.CodigoFornecedor ?? null) as string | null,
    fornecedorId: o.fornecedorId != null || o.FornecedorId != null
      ? Number(o.fornecedorId ?? o.FornecedorId)
      : null,
    fornecedorNome: (o.fornecedorNome ?? o.FornecedorNome ?? null) as string | null,
    ativo: Boolean(o.ativo ?? o.Ativo ?? true),
  };
}

export type NotificacaoLista = {
  id: number;
  pedidoId: number;
  pedidoItemId: number;
  fornecedorId: number | null;
  fornecedorNome: string | null;
  produtoId: number | null;
  produtoNome: string | null;
  loja: string;
  pedidoExternoId: string;
  nomeCliente: string;
  status: string;
  mensagemTexto: string;
  whatsAppMessageId: string | null;
  erro: string | null;
  criadoEm: string;
  enviadoEm: string | null;
};

export function normalizeNotificacao(o: Record<string, unknown>): NotificacaoLista {
  return {
    id: Number(o.id ?? o.Id ?? 0),
    pedidoId: Number(o.pedidoId ?? o.PedidoId ?? 0),
    pedidoItemId: Number(o.pedidoItemId ?? o.PedidoItemId ?? 0),
    fornecedorId: o.fornecedorId != null || o.FornecedorId != null
      ? Number(o.fornecedorId ?? o.FornecedorId)
      : null,
    fornecedorNome: (o.fornecedorNome ?? o.FornecedorNome ?? null) as string | null,
    produtoId: o.produtoId != null || o.ProdutoId != null
      ? Number(o.produtoId ?? o.ProdutoId)
      : null,
    produtoNome: (o.produtoNome ?? o.ProdutoNome ?? null) as string | null,
    loja: String(o.loja ?? o.Loja ?? ""),
    pedidoExternoId: String(o.pedidoExternoId ?? o.PedidoExternoId ?? ""),
    nomeCliente: String(o.nomeCliente ?? o.NomeCliente ?? ""),
    status: String(o.status ?? o.Status ?? ""),
    mensagemTexto: String(o.mensagemTexto ?? o.MensagemTexto ?? ""),
    whatsAppMessageId: (o.whatsAppMessageId ?? o.WhatsAppMessageId ?? null) as string | null,
    erro: (o.erro ?? o.Erro ?? null) as string | null,
    criadoEm: String(o.criadoEm ?? o.CriadoEm ?? ""),
    enviadoEm: (o.enviadoEm ?? o.EnviadoEm ?? null) as string | null,
  };
}
