using SistemaEtiquetas.Domain.Entities;

namespace SistemaEtiquetas.API.Services
{
    public class EtiquetaService
    {
        public string GerarZpl(Pedido pedido, PedidoItem item)
        {
            var data = pedido.DataPedido.ToString("dd/MM/yyyy");
            var nome = pedido.NomeCliente ?? "";
            var modelo = item.Produto ?? "";
            var cor = item.Cor ?? "N/A";
            var tamanho = item.Tamanho ?? "N/A";
            var cpf = pedido.ClienteCpf ?? "";
            var sku = item.SKU ?? "N/A";

            // Limitar comprimento dos campos para caber na etiqueta
            var linha1Nome = nome.Length > 30 ? nome.Substring(0, 30) : nome;
            var linha2Modelo = modelo.Length > 35 ? modelo.Substring(0, 35) : modelo;
            var linha3Cor = cor.Length > 20 ? cor.Substring(0, 20) : cor;
            var linha4Tam = tamanho.Length > 10 ? tamanho.Substring(0, 10) : tamanho;
            var linha5Cpf = cpf.Length > 20 ? cpf.Substring(0, 20) : cpf;
            var linha6Sku = sku.Length > 20 ? sku.Substring(0, 20) : sku;

            var zpl = $@"^XA
^CF0,25

^FO20,20^FDNOME: {linha1Nome}^FS
^FO300,20^FDDATA: {data}^FS

^FO20,70^FDMODELO: {linha2Modelo}^FS
^FO20,110^FDCOR: {linha3Cor}^FS
^FO200,110^FDTAM: {linha4Tam}^FS

^FO20,160^FDCPF: {linha5Cpf}^FS
^FO300,160^FDSKU: {linha6Sku}^FS

^XZ";

            return zpl;
        }
    }
}
