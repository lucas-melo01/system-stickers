using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEtiquetas.Domain.Entities;

public class NuvemshopInstalacao
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>ID da loja Nuvemshop (user_id do OAuth).</summary>
    public long StoreId { get; set; }

    public string AccessToken { get; set; } = "";

    /// <summary>Label de vendedor nas etiquetas/relatórios ("Resume Modas" | "DonnaKora").</summary>
    public string Vendedor { get; set; } = "";

    public string? Scopes { get; set; }

    public bool Ativo { get; set; } = true;

    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
