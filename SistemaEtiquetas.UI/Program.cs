using Microsoft.EntityFrameworkCore;
using SistemaEtiquetas.API.Services;
using SistemaEtiquetas.Infrastructure.Data;
using SistemaEtiquetas.UI.Services;
using System.Diagnostics;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Configure culture to pt-BR for currency display (R$ instead of £)
var ptBr = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = ptBr;
CultureInfo.DefaultThreadCurrentUICulture = ptBr;

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddScoped<EtiquetaService>();
builder.Services.AddScoped<ImpressaoService>();

// Registrar HttpClient para chamadas à API
var apiUrl = builder.Configuration["Api:Url"] ?? "http://localhost:5000";
builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri(apiUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Registrar ApiService
builder.Services.AddScoped<ApiService>();

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(databaseUrl));

var app = builder.Build();

// In development, start the API project as a background process so webhooks can be received
Process? apiProcess = null;
if (app.Environment.IsDevelopment())
{
    try
    {
        // locate the API project folder relative to the current working directory
        var apiProjectPath = Path.Combine(Directory.GetCurrentDirectory(), "SistemaEtiquetas.API");
        if (Directory.Exists(apiProjectPath))
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{apiProjectPath}\" --no-build",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = apiProjectPath
            };

            apiProcess = Process.Start(psi);

            if (apiProcess != null)
            {
                // optionally log output asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var stdout = apiProcess.StandardOutput;
                        var stderr = apiProcess.StandardError;

                        while (!stdout.EndOfStream)
                        {
                            var line = await stdout.ReadLineAsync();
                            if (line != null) Console.WriteLine("[API] " + line);
                        }

                        while (!stderr.EndOfStream)
                        {
                            var line = await stderr.ReadLineAsync();
                            if (line != null) Console.Error.WriteLine("[API-ERR] " + line);
                        }
                    }
                    catch { }
                });
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to start API in background: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Ensure database exists and apply migrations so the UI sees the same schema
// 🚨 COMENTADO TEMPORARIAMENTE: Se Supabase estiver lento/fora, descomente depois
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Aviso: Não conseguiu aplicar migrations: {ex.Message}");
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

// When application is stopping, ensure we kill the background API process if we started it
var lifetime = app.Lifetime;
lifetime.ApplicationStopping.Register(() =>
{
    try
    {
        if (apiProcess != null && !apiProcess.HasExited)
        {
            apiProcess.Kill(true);
            apiProcess.Dispose();
        }
    }
    catch { }
});

app.Run();
