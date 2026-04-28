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

public class PedidoItemRowDto
{
    public int PedidoId { get; set; }
    public int PedidoItemId { get; set; }
    public DateTime DataPedido { get; set; }
    public string PedidoExternoId { get; set; } = string.Empty;
    public string NomeCliente { get; set; } = string.Empty;
    public string? ClienteCpf { get; set; }
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
