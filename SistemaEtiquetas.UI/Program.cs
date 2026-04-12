using Microsoft.EntityFrameworkCore;
using SistemaEtiquetas.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Configure Postgres (Supabase) connection. Prefer environment variable "DATABASE_URL".
// Fallback to the user-provided Supabase connection string if not set.
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? "Host=db.pvmdtjxixrpckfdbrhpz.supabase.co;Username=postgres;Password=%/i_EjK/eq5EV2$;Database=postgres;Port=5432;SSL Mode=Require;Trust Server Certificate=true";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(databaseUrl));

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
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();
