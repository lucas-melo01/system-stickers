namespace SistemaEtiquetas.API.DTO
{
    public class AddPedidoDto
    {
        public string IdExterno { get; set; }
        public string NomeCliente { get; set; }
        public string ClienteCpf { get; set; }
        public DateTime DataPedido { get; set; }
        public List<AddPedidoItemDto> Itens { get; set; } = new();
    }

    public class AddPedidoItemDto
    {
        public string Produto { get; set; }
        public string SKU { get; set; }
        public string Cor { get; set; }
        public string Tamanho { get; set; }
        public int Quantidade { get; set; }
    }
}
