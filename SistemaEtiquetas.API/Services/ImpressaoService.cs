using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace SistemaEtiquetas.API.Services
{
    public class ImpressaoService
    {
        private readonly string _ip;
        private readonly int _porta;

        public ImpressaoService(IConfiguration config)
        {
            _ip = config["Impressora:Ip"];
            _porta = int.TryParse(config["Impressora:Porta"], out var p) ? p : 9100;
        }

        public bool Imprimir(string zpl, out string erro)
        {
            try
            {
                using var client = new TcpClient();
                client.Connect(_ip, _porta);
                using var stream = client.GetStream();

                var bytes = Encoding.UTF8.GetBytes(zpl);

                stream.Write(bytes, 0, bytes.Length);
                stream.Flush();

                erro = null;
                return true;
            }
            catch (Exception ex)
            {
                erro = ex.Message;
                return false;
            }
        }
    }
}
