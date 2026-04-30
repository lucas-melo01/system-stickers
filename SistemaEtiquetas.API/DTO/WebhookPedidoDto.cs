using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SistemaEtiquetas.API.DTO
{
    /// <summary>
    /// Aceita no JSON <c>numero</c> como string ou número (valor usado como número do pedido no sistema).
    /// </summary>
    internal sealed class WebhookNumeroPedidoConverter : JsonConverter<string?>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.Number => reader.TryGetInt64(out var l)
                    ? l.ToString(CultureInfo.InvariantCulture)
                    : reader.GetDecimal().ToString(CultureInfo.InvariantCulture),
                JsonTokenType.Null => null,
                _ => null,
            };
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            if (value == null) writer.WriteNullValue();
            else writer.WriteStringValue(value);
        }
    }

    public class WebhookPedidoDto
    {
        public long id { get; set; }

        [JsonPropertyName("numero")]
        [JsonConverter(typeof(WebhookNumeroPedidoConverter))]
        public string? numero { get; set; }
        public DateTime data_criacao { get; set; }
        public string tipo { get; set; }
        public string marketplace_origem { get; set; }
        public ClienteDto cliente { get; set; }
        public List<ItemDto> itens { get; set; }
        public List<EnvioDto> envios { get; set; }
        public List<PagamentoDto> pagamentos { get; set; }
        public decimal? valor_envio { get; set; }
        public SituacaoDto situacao { get; set; }
    }

    public class ClienteDto
    {
        public string nome { get; set; }
        public string cpf { get; set; }
    }

    public class ItemDto
    {
        /// <summary>Id da linha do pedido na plataforma.</summary>
        public long id { get; set; }

        [JsonPropertyName("produto_id")]
        public long produto_id { get; set; }

        public string nome { get; set; }
        public string sku { get; set; }
        public int quantidade { get; set; }
    }

    public class EnvioDto
    {
        public decimal? valor { get; set; }
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
