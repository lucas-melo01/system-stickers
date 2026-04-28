using SistemaEtiquetas.Domain;
using SistemaEtiquetas.Domain.Entities;

namespace SistemaEtiquetas.API.Services
{
    public class EtiquetaService
    {
        // Etiqueta física: 60 mm × 40 mm em impressora a 203 dpi (8 dots/mm).
        //   Largura : 60 mm × 8 = 480 dots  → ^PW480
        //   Altura  : 40 mm × 8 = 320 dots  → ^LL320
        //
        // NOTA: ^MNY e ^MMT foram deliberadamente omitidos.
        // São comandos de configuração de firmware que forçam re-calibração do
        // tracking de gap cada vez que são recebidos, desperdiçando 1-2 etiquetas
        // em branco antes de cada impressão. A impressora já está correctamente
        // configurada (tracking por gap, modo separar), pelo que não é necessário
        // reenviá-los a cada job.
        //
        // ^LH0,0 e ^LS0 são seguros por job — apenas fixam a origem e o shift.
        public string GerarZpl(Pedido pedido, PedidoItem item)
        {
            var data = TimeZoneBrasil.DeUtcParaBrasilia(pedido.DataPedido).ToString("dd/MM/yyyy HH:mm");
            var nome = pedido.NomeCliente ?? "";
            var modelo = item.Produto ?? "";
            var cor = item.Cor ?? "N/A";
            var tamanho = item.Tamanho ?? "N/A";
            var cpf = pedido.ClienteCpf ?? "";

            var linha1Nome   = nome.Length    > 30 ? nome.Substring(0, 30)    : nome;
            var linha2Modelo = modelo.Length  > 35 ? modelo.Substring(0, 35)  : modelo;
            var linha3Cor    = cor.Length     > 20 ? cor.Substring(0, 20)     : cor;
            var linha4Tam    = tamanho.Length > 10 ? tamanho.Substring(0, 10) : tamanho;
            var linha5Cpf    = cpf.Length     > 20 ? cpf.Substring(0, 20)     : cpf;

            var zpl = $@"^XA
^LH0,0
^LS0
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
