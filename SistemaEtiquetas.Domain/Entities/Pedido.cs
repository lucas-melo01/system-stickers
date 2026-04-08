using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaEtiquetas.Domain.Entities
{
    public class Pedido
    {
        public int Id { get; set; }

        public string PedidoExternoId { get; set; }

        public string NomeCliente { get; set; }

        public DateTime DataPedido { get; set; }

        public DateTime DataCriacao { get; set; } = DateTime.Now;

        public List<PedidoItem> Itens { get; set; } = new();
    }
}
