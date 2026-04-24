using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

try
{
    int porta = 5000;
    string url = $"http://localhost:{porta}";

    var baseDir = AppContext.BaseDirectory;
    var caminhoApp = Path.Combine(baseDir, "SistemaEtiquetas.UI.exe");

    // Verifica se o executável do UI existe
    if (!File.Exists(caminhoApp))
    {
        return;
    }

    bool portaEmUso = IsPortInUse(porta);

    // Se a porta NÃO estiver em uso, significa que o UI ainda não iniciou
    if (!portaEmUso)
    {
        var processoUI = Process.Start(new ProcessStartInfo
        {
            FileName = caminhoApp,
            Arguments = url,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });

        if (processoUI == null)
        {
            return;
        }

        // Aguarda aplicação subir
        Thread.Sleep(5000);
    }

    // Abre navegador padrão
    Process.Start(new ProcessStartInfo
    {
        FileName = url,
        UseShellExecute = true
    });
}
catch
{
    // silencioso para cliente não ver stack trace feia
}


// ---------------- FUNÇÕES AUXILIARES ----------------

bool IsPortInUse(int port)
{
    try
    {
        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        listener.Stop();
        return false;
    }
    catch
    {
        return true;
    }
}