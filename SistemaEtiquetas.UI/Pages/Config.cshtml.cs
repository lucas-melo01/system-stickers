using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SistemaEtiquetas.Domain.Entities;
using SistemaEtiquetas.Infrastructure.Data;
using System.Net.Sockets;

namespace SistemaEtiquetas.UI.Pages
{
    public class ConfigModel : PageModel
    {
        private readonly AppDbContext _db;

        [BindProperty]
        public string ImpressoraIp { get; set; }

        [BindProperty]
        public int ImpressoraPorta { get; set; }

        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }
        public string TestMessage { get; set; }
        public bool TestSuccess { get; set; }

        public ConfigModel(AppDbContext db)
        {
            _db = db;
        }

        public async Task OnGet()
        {
            var config = await _db.Configuracoes.FirstOrDefaultAsync();
            if (config != null)
            {
                ImpressoraIp = config.ImpressoraIp;
                ImpressoraPorta = config.ImpressoraPorta;
            }
            else
            {
                ImpressoraIp = "127.0.0.1";
                ImpressoraPorta = 9100;
            }
        }

        public async Task<IActionResult> OnPostSaveConfig()
        {
            try
            {
                var config = await _db.Configuracoes.FirstOrDefaultAsync();

                if (config == null)
                {
                    config = new Configuracao();
                    _db.Configuracoes.Add(config);
                }

                config.ImpressoraIp = ImpressoraIp;
                config.ImpressoraPorta = ImpressoraPorta;

                await _db.SaveChangesAsync();
                SuccessMessage = "✅ Configurações de impressora salvas com sucesso!";

                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"❌ Erro ao salvar: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostTestConnection(string ip, int porta)
        {
            try
            {
                using var client = new TcpClient();
                client.ConnectAsync(ip, porta).Wait(TimeSpan.FromSeconds(5));

                if (client.Connected)
                {
                    return new JsonResult(new { success = true, message = "✅ Conexão bem-sucedida!" });
                }
            }
            catch (OperationCanceledException)
            {
                return new JsonResult(new { success = false, message = "❌ Timeout: impressora não respondeu" });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"❌ Erro: {ex.Message}" });
            }

            return new JsonResult(new { success = false, message = "❌ Falha na conexão" });
        }
    }
}

