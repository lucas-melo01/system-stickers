using SistemaEtiquetas.Domain;
using SistemaEtiquetas.Domain.Entities;

namespace SistemaEtiquetas.UI.Services
{
    public class EtiquetaService
    {
        public string GerarZpl(Pedido pedido, PedidoItem item)
        {
            var data = TimeZoneBrasil.DeUtcParaBrasilia(pedido.DataPedido).ToString("dd/MM/yyyy HH:mm");
            var nome = TextoEtiqueta.RemoverAcentos(pedido.NomeCliente);
            var modelo = TextoEtiqueta.RemoverAcentos(item.Produto);
            var cor = TextoEtiqueta.RemoverAcentos(item.Cor);
            cor = string.IsNullOrWhiteSpace(cor) ? "N/A" : cor;
            var tamanho = TextoEtiqueta.RemoverAcentos(item.Tamanho);
            tamanho = string.IsNullOrWhiteSpace(tamanho) ? "N/A" : tamanho;
            var cpf = TextoEtiqueta.RemoverAcentos(pedido.ClienteCpf);
            var codForn = TextoEtiqueta.RemoverAcentos(item.SKU);

            // Limitar comprimento dos campos para caber na etiqueta
            var linha1Nome = nome.Length > 30 ? nome.Substring(0, 30) : nome;
            var linhaCodForn = codForn.Length > 28 ? codForn.Substring(0, 28) : codForn;
            var linha2Modelo = modelo.Length > 35 ? modelo.Substring(0, 35) : modelo;
            var linha3Cor = cor.Length > 20 ? cor.Substring(0, 20) : cor;
            var linha4Tam = tamanho.Length > 10 ? tamanho.Substring(0, 10) : tamanho;
            var linha5Cpf = cpf.Length > 20 ? cpf.Substring(0, 20) : cpf;

            var zpl = $@"^XA
^LH0,0
^LS0
^PW480
^LL320
^CF0,24

^FO20,20^FDNOME: {linha1Nome}^FS
^FO20,50^FDDATA: {data}^FS

^FO20,80^FDMODELO: {linha2Modelo}^FS

^FO20,115^FDCOR: {linha3Cor}^FS
^FO250,115^FDTAM: {linha4Tam}^FS

^FO20,150^FDCPF: {linha5Cpf}^FS
^FO20,185^FDCOD.FORN.: {linhaCodForn}^FS

^XZ";

            return zpl;
        }
    }
}
