using System;
using System.Collections.Generic;

namespace SistemaEtiquetas.API.DTO
{
    public class WebhookPedidoDto
    {
        public long id { get; set; }
        public DateTime data_criacao { get; set; }
        public string tipo { get; set; }
        public string marketplace_origem { get; set; }
        public ClienteDto cliente { get; set; }
        public List<ItemDto> itens { get; set; }
        public List<EnvioDto> envios { get; set; }
        public List<PagamentoDto> pagamentos { get; set; }
        public decimal valor_envio { get; set; }
        public SituacaoDto situacao { get; set; }
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

    public class EnvioDto
    {
        public decimal valor { get; set; }
        public FormaEnvioDto forma_envio { get; set; }
    }

    public class FormaEnvioDto
    {
        public string nome { get; set; }
    }

    public class PagamentoDto
    {
        public FormaPagamentoDto forma_pagamento { get; set; }
    }

    public class FormaPagamentoDto
    {
        public string nome { get; set; }
    }

    public class SituacaoDto
    {
        public string codigo { get; set; }
    }
}
