using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEtiquetas.Domain.Entities
{
    public class PedidoItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int PedidoId { get; set; }
        public Pedido Pedido { get; set; }

        public string Produto { get; set; } = string.Empty;

        public string SKU { get; set; } = string.Empty;

        public string? Cor { get; set; }

        public string? Tamanho { get; set; }

        public int Quantidade { get; set; }

        public bool Impresso { get; set; } = false;

        // Valores monetários
        public decimal ValorCusto { get; set; } = 0;
        public decimal ValorVenda { get; set; } = 0;
    }
}
