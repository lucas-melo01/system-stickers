using System.IO;
using Microsoft.EntityFrameworkCore;
using SistemaEtiquetas.Infrastructure.Data;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Resolve a database path so the UI and API can point to the same file when present.
string ResolveDatabasePath()
{
    // Prefer the API project's database so the UI and API share the same SQLite file
    var candidates = new[]
    {
        // sibling API project (when running from solution root)
        Path.Combine(Directory.GetCurrentDirectory(), "SistemaEtiquetas.API", "database.db"),
        // when running from bin folder, go up to the project root and then to API
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "SistemaEtiquetas.API", "database.db")),
        // fallback to bin folder
        Path.Combine(AppContext.BaseDirectory, "database.db")
    };

    foreach (var c in candidates)
    {
        try
        {
            if (File.Exists(c))
                return c;
        }
        catch { }
    }

    // default: create local database next to AppContext.BaseDirectory
    var defaultPath = Path.Combine(AppContext.BaseDirectory, "database.db");
    return defaultPath;
}

var dbPath = ResolveDatabasePath();
// Log resolved database path so it's clear which file the app will use at runtime
Console.WriteLine($"Resolved DB path: {dbPath}");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
        app.UseHsts();
}

// Ensure database exists and apply migrations so the UI sees the same schema
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // After applying migrations, print the schema of the 'Pedidos' table so we can confirm
    try
    {
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA table_info('Pedidos');";
        using var reader = cmd.ExecuteReader();
        Console.WriteLine("Pedidos table schema:");
        while (reader.Read())
        {
            var name = reader[1];
            var type = reader[2];
            Console.WriteLine($" - {name} ({type})");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to inspect Pedidos schema: {ex.Message}");
    }
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();
