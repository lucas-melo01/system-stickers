namespace SistemaEtiquetas.API.DTO;

public class CreatePedidoRequest
{
    public string PedidoExternoId { get; set; } = string.Empty;
    public string NomeCliente { get; set; } = string.Empty;
    public string? ClienteCpf { get; set; }
    public DateTime DataPedido { get; set; }
    public string? Vendedor { get; set; }
    public string? TipoEnvio { get; set; }
    public string? FormaPagamento { get; set; }
    public decimal ValorFrete { get; set; }
    public List<CreatePedidoItemRequest> Itens { get; set; } = new();
}

public class CreatePedidoItemRequest
{
    public string Produto { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? Cor { get; set; }
    public string? Tamanho { get; set; }
    public int Quantidade { get; set; } = 1;
    public decimal ValorCusto { get; set; }
    public decimal ValorVenda { get; set; }
}

public class UpdatePedidoItemRequest
{
    public string Produto { get; set; } = string.Empty;
    public string? Cor { get; set; }
    public string? Tamanho { get; set; }
    public int Quantidade { get; set; } = 1;
}

public class UpdatePedidoManualRequest
{
    public string PedidoExternoId { get; set; } = string.Empty;
    public string NomeCliente { get; set; } = string.Empty;
    public string? ClienteCpf { get; set; }
    public DateTime DataPedido { get; set; }
    public string? TipoEnvio { get; set; }
    public string? FormaPagamento { get; set; }
    public decimal ValorFrete { get; set; }
}

public class PedidoItemRowDto
{
    public int PedidoId { get; set; }
    public int PedidoItemId { get; set; }
    public DateTime DataPedido { get; set; }
    public string PedidoExternoId { get; set; } = string.Empty;
    /// <summary>Pedido criado na UI (sem payload de webhook).</summary>
    public bool EhPedidoManual { get; set; }
    public string NomeCliente { get; set; } = string.Empty;
    public string? ClienteCpf { get; set; }
    public string? TipoEnvio { get; set; }
    public string? FormaPagamento { get; set; }
    public decimal ValorFrete { get; set; }
    public string Produto { get; set; } = string.Empty;
    /// <summary>Coluna <c>SKU</c> na base (código do fornecedor / MPN).</summary>
    public string CodigoFornecedor { get; set; } = string.Empty;
    public string? Cor { get; set; }
    public string? Tamanho { get; set; }
    public int Quantidade { get; set; }
    public bool Impresso { get; set; }
}

public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class UsuarioSistemaDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Nome { get; set; }
    public string Perfil { get; set; } = "Operador";
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class AtualizarUsuarioRequest
{
    public string? Perfil { get; set; }
    public bool? Ativo { get; set; }
}

public class ProvisionUsuarioRequest
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Perfil { get; set; } = "Operador";
}

public class PendenteImpressaoDto
{
    public int ItemId { get; set; }
    public int Quantidade { get; set; }
}

public class MarcarImpressoLoteRequest
{
    public List<int> ItemIds { get; set; } = new();
}

public class MarcarImpressoLoteResponse
{
    public int Marcados { get; set; }
    public List<int> MissingItemIds { get; set; } = new();
}

// ─── Cadastros (Fornecedores / Produtos) ───

public class FornecedorDto
{
    public int Id { get; set; }
    public string NomeRazaoSocial { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string WhatsApp { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public int ProdutosVinculados { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}

public class CreateFornecedorRequest
{
    public string NomeRazaoSocial { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string WhatsApp { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public List<int>? ProdutoIds { get; set; }
}

public class UpdateFornecedorRequest
{
    public string NomeRazaoSocial { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string WhatsApp { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
}

public class VincularProdutosFornecedorRequest
{
    public List<int> ProdutoIds { get; set; } = new();
}

public class ProdutoDto
{
    public int Id { get; set; }
    public string Loja { get; set; } = string.Empty;
    public long ProdutoIdLojaIntegrada { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? CodigoFornecedor { get; set; }
    public int? FornecedorId { get; set; }
    public string? FornecedorNome { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}

public class CreateProdutoRequest
{
    public string Loja { get; set; } = string.Empty;
    public long ProdutoIdLojaIntegrada { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? CodigoFornecedor { get; set; }
    public bool Ativo { get; set; } = true;
}

public class UpdateProdutoRequest
{
    public string Nome { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? CodigoFornecedor { get; set; }
    public bool Ativo { get; set; } = true;
}

public class NotificacaoFornecedorDto
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public int PedidoItemId { get; set; }
    public int? FornecedorId { get; set; }
    public string? FornecedorNome { get; set; }
    public int? ProdutoId { get; set; }
    public string? ProdutoNome { get; set; }
    public string Loja { get; set; } = string.Empty;
    public string PedidoExternoId { get; set; } = string.Empty;
    public string NomeCliente { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string MensagemTexto { get; set; } = string.Empty;
    public string? WhatsAppMessageId { get; set; }
    public string? Erro { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? EnviadoEm { get; set; }
}
