using System.Diagnostics;

namespace SistemaEtiquetas.API.Services
{
    public class ImpressaoService
    {
        public bool Imprimir(string zpl, out string erro)
        {
            try
            {
                var caminho = Path.Combine(Directory.GetCurrentDirectory(), "etiqueta.txt");

                File.WriteAllText(caminho, zpl);

                var psi = new ProcessStartInfo
                {
                    FileName = caminho,
                    Verb = "print",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(psi);

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
