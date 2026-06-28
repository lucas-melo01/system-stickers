using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEtiquetas.Domain.Entities;

public class NotificacaoFornecedor
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int PedidoId { get; set; }

    public Pedido Pedido { get; set; } = null!;

    public int PedidoItemId { get; set; }

    public PedidoItem PedidoItem { get; set; } = null!;

    public int? FornecedorId { get; set; }

    public Fornecedor? Fornecedor { get; set; }

    public int? ProdutoId { get; set; }

    public Produto? Produto { get; set; }

    public LojaOrigem Loja { get; set; }

    public string PedidoExternoId { get; set; } = string.Empty;

    public string NomeCliente { get; set; } = string.Empty;

    public StatusNotificacaoFornecedor Status { get; set; } = StatusNotificacaoFornecedor.Pendente;

    public string MensagemTexto { get; set; } = string.Empty;

    public string? WhatsAppMessageId { get; set; }

    public string? Erro { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public DateTime? EnviadoEm { get; set; }
}
