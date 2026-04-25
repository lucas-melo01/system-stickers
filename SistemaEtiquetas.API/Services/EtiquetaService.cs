using SistemaEtiquetas.Domain.Entities;

namespace SistemaEtiquetas.API.Services
{
    public class EtiquetaService
    {
        // Etiqueta física: 60 mm × 40 mm em impressora a 300 dpi (12 dots/mm).
        //   Largura: 60 mm * 12 = 720 dots
        //   Altura : 40 mm * 12 = 480 dots
        //
        // Comandos defensivos enviados a cada job para evitar que estado interno
        // anterior da impressora (deixado por software/driver de terceiros) faça
        // o conteúdo desalinhar ou transbordar para a etiqueta seguinte:
        //   ^MNY  → tracking pelo gap entre etiquetas
        //   ^MMT  → modo tear-off (etiqueta em bobina destacável)
        //   ^LH0,0 + ^LS0 → origem (0,0) e label-shift = 0
        //   ^PW720 + ^LL480 → tamanho físico real da etiqueta
        //
        // Se um dia for preciso suportar 203 dpi, basta dividir todas as
        // coordenadas e tamanhos por 1,4778 (300/203) ou parametrizar por config.
        public string GerarZpl(Pedido pedido, PedidoItem item)
        {
            var data = pedido.DataPedido.ToString("dd/MM/yyyy");
            var nome = pedido.NomeCliente ?? "";
            var modelo = item.Produto ?? "";
            var cor = item.Cor ?? "N/A";
            var tamanho = item.Tamanho ?? "N/A";
            var cpf = pedido.ClienteCpf ?? "";
            var sku = item.SKU ?? "N/A";

            // Limites de truncamento mantidos — fonte de 36 dots a 300 dpi tem
            // a mesma largura física que 24 dots a 203 dpi, portanto cabe igual.
            var linha1Nome = nome.Length > 30 ? nome.Substring(0, 30) : nome;
            var linha2Modelo = modelo.Length > 35 ? modelo.Substring(0, 35) : modelo;
            var linha3Cor = cor.Length > 20 ? cor.Substring(0, 20) : cor;
            var linha4Tam = tamanho.Length > 10 ? tamanho.Substring(0, 10) : tamanho;
            var linha5Cpf = cpf.Length > 20 ? cpf.Substring(0, 20) : cpf;
            var linha6Sku = sku.Length > 20 ? sku.Substring(0, 20) : sku;

            var zpl = $@"^XA
^MNY
^MMT
^LH0,0
^LS0
^PW720
^LL480
^CF0,36

^FO30,30^FDNOME: {linha1Nome}^FS
^FO30,82^FDDATA: {data}^FS

^FO30,142^FDMODELO: {linha2Modelo}^FS

^FO30,202^FDCOR: {linha3Cor}^FS
^FO375,202^FDTAM: {linha4Tam}^FS

^FO30,262^FDCPF: {linha5Cpf}^FS

^FO30,322^FDSKU: {linha6Sku}^FS

^XZ";

            return zpl;
        }
    }
}
