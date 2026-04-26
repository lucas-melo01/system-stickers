using SistemaEtiquetas.Domain;
using SistemaEtiquetas.Domain.Entities;

namespace SistemaEtiquetas.UI.Services
{
    public class EtiquetaService
    {
        public string GerarZpl(Pedido pedido, PedidoItem item)
        {
            var data = TimeZoneBrasil.DeUtcParaBrasilia(pedido.DataPedido).ToString("dd/MM/yyyy");
            var nome = pedido.NomeCliente ?? "";
            var modelo = item.Produto ?? "";
            var cor = item.Cor ?? "N/A";
            var tamanho = item.Tamanho ?? "N/A";
            var cpf = pedido.ClienteCpf ?? "";

            // Limitar comprimento dos campos para caber na etiqueta
            var linha1Nome = nome.Length > 30 ? nome.Substring(0, 30) : nome;
            var linha2Modelo = modelo.Length > 35 ? modelo.Substring(0, 35) : modelo;
            var linha3Cor = cor.Length > 20 ? cor.Substring(0, 20) : cor;
            var linha4Tam = tamanho.Length > 10 ? tamanho.Substring(0, 10) : tamanho;
            var linha5Cpf = cpf.Length > 20 ? cpf.Substring(0, 20) : cpf;

            var zpl = $@"^XA
^PW480
^LL320
^CF0,24

^FO20,20^FDNOME: {linha1Nome}^FS
^FO20,55^FDDATA: {data}^FS

^FO20,95^FDMODELO: {linha2Modelo}^FS

^FO20,135^FDCOR: {linha3Cor}^FS
^FO250,135^FDTAM: {linha4Tam}^FS

^FO20,175^FDCPF: {linha5Cpf}^FS

^XZ";

            return zpl;
        }
    }
}
