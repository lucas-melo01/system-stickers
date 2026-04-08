using SistemaEtiquetas.Domain.Entities;

namespace SistemaEtiquetas.API.Services
{
    public class EtiquetaService
    {
        public string GerarZpl(Pedido pedido, PedidoItem item)
        {
            var data = pedido.DataPedido.ToString("dd/MM");

            var produto = item.Produto ?? "";
            var cliente = pedido.NomeCliente ?? "";

            var linha1 = cliente.Length > 25 ? cliente.Substring(0, 25) : cliente;
            var linha2 = produto.Length > 25 ? produto.Substring(0, 25) : produto;

            var zpl = $@"
^XA
^PW400
^LL240

^FO20,20^A0N,30,30^FD{linha1}^FS
^FO20,60^A0N,25,25^FD{linha2}^FS
^FO20,100^A0N,25,25^FDData: {data}^FS

^XZ";

            return zpl;
        }
    }
}
