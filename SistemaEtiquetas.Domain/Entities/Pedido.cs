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

        public DateTime DataPedido { get; set; }

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        public List<PedidoItem> Itens { get; set; } = new();
    }
}
