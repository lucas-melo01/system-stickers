using System.Text.Json.Serialization;

namespace SistemaEtiquetas.API.DTO;

public class WebhookProdutoDto
{
    public long id { get; set; }

    public string? nome { get; set; }

    public string? sku { get; set; }

    public string? mpn { get; set; }

    public bool ativo { get; set; } = true;

    public bool removido { get; set; }
}
