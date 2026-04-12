
using System;
using System.Collections.Generic;

namespace SistemaEtiquetas.API.DTO
{
    public class WebhookPedidoDto
    {
        public long id { get; set; }
        public DateTime data_criacao { get; set; }
        public ClienteDto cliente { get; set; }
        public List<ItemDto> itens { get; set; }
    }

    public class ClienteDto
    {
        public string nome { get; set; }
        public string cpf { get; set; }
    }

    public class ItemDto
    {
        public string nome { get; set; }
        public string sku { get; set; }
        public int quantidade { get; set; }
    }
}
