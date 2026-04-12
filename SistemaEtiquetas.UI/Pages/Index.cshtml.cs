using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaEtiquetas.Domain.Entities;
using SistemaEtiquetas.Infrastructure.Data;

namespace SistemaEtiquetas.UI.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public List<Pedido> Pedidos { get; set; }

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public async Task OnGet()
    {
        Pedidos = await _db.Pedidos
            .Include(p => p.Itens)
            .ToListAsync();
    }

    // Método para aplicar migrações em runtime
    public async Task OnGetMigrate()
    {
        using var scope = HttpContext.RequestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
}