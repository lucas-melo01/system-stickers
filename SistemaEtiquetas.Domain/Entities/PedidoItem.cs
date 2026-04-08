using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaEtiquetas.Domain.Entities
{
    public class PedidoItem
    {
        public int Id { get; set; }

        public int PedidoId { get; set; }
        public Pedido Pedido { get; set; }

        public string Produto { get; set; }

        public string SKU { get; set; }

        public string? Cor { get; set; }

        public string? Tamanho { get; set; }

        public int Quantidade { get; set; }

        public bool Impresso { get; set; } = false;

        public bool Erro { get; set; } = false;

        public string? ErroMensagem { get; set; }
    }
}
