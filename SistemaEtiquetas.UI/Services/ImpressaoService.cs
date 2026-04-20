using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using SistemaEtiquetas.Infrastructure.Data;

namespace SistemaEtiquetas.UI.Services
{
    public class ImpressaoService
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _db;

        public ImpressaoService(IConfiguration config, AppDbContext db = null)
        {
            _config = config;
            _db = db;
        }

        public bool Imprimir(string zpl, out string erro)
        {
            try
            {
                // Tentar obter configurações do banco de dados primeiro
                string ip = null;
                int porta = 9100;

                if (_db != null)
                {
                    var config = _db.Configuracoes.FirstOrDefault();
                    if (config != null)
                    {
                        ip = config.ImpressoraIp;
                        porta = config.ImpressoraPorta;
                    }
                }

                // Fallback para appsettings se não encontrar no banco
                if (string.IsNullOrEmpty(ip))
                {
                    ip = _config["Impressora:Ip"];
                    if (!int.TryParse(_config["Impressora:Porta"], out porta))
                        porta = 9100;
                }

                if (string.IsNullOrEmpty(ip))
                {
                    erro = "IP da impressora não configurado";
                    return false;
                }

                using var client = new TcpClient();
                client.Connect(ip, porta);
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
