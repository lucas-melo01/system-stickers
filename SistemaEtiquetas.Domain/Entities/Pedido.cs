using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEtiquetas.Domain.Entities
{
    public class Pedido
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string PedidoExternoId { get; set; }

        public string NomeCliente { get; set; }

        // CPF do cliente recebido no webhook
        public string? ClienteCpf { get; set; }

        // Vendedor (Resume, DonnaKora, Manual, etc)
        public string? Vendedor { get; set; }

        public DateTime DataPedido { get; set; }

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        // Forma de pagamento
        public string? FormaPagamento { get; set; }

        // Tipo de envio (Pac, Sedex, etc)
        public string? TipoEnvio { get; set; }

        // Valor do frete
        public decimal ValorFrete { get; set; } = 0;

        // JSON completo recebido do webhook para auditoria e mapeamento
        public string? jsonWebhook { get; set; }

        public List<PedidoItem> Itens { get; set; } = new();
    }
}
