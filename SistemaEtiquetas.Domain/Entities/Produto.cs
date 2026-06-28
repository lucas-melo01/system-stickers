using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEtiquetas.Domain.Entities;

public class Produto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public LojaOrigem Loja { get; set; }

    public long ProdutoIdLojaIntegrada { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Sku { get; set; }

    /// <summary>MPN / código do fornecedor na Loja Integrada.</summary>
    public string? CodigoFornecedor { get; set; }

    /// <summary>Vínculo manual via tela de Fornecedores; nunca alterado pelo webhook LI.</summary>
    public int? FornecedorId { get; set; }

    public Fornecedor? Fornecedor { get; set; }

    public bool Ativo { get; set; } = true;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}
