using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEtiquetas.Domain.Entities;

public class Fornecedor
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string NomeRazaoSocial { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string WhatsApp { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;

    public List<Produto> Produtos { get; set; } = new();
}
