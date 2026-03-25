using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaEtiquetas.Domain.Entities
{
    public class Pedido
    {
        public int Id { get; set; }

        public string PedidoExternoId { get; set; }

        public string FornecedorId { get; set; }

        public string NomeCliente { get; set; }

        public string Produto { get; set; }

        public string Cor { get; set; }

        public string Tamanho { get; set; }

        public DateTime DataPedido { get; set; }

        public bool Impresso { get; set; } = false;

        public DateTime DataCriacao { get; set; } = DateTime.Now;
    }
}
