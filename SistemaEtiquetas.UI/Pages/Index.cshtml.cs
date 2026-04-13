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

    [BindProperty(SupportsGet = true)]
    public string SearchTerm { get; set; }

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public async Task OnGet()
    {
        var query = _db.Pedidos
            .Include(p => p.Itens)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var searchLower = SearchTerm.ToLower().Trim();
            query = query.Where(p =>
                p.Id.ToString().Contains(searchLower) ||
                p.NomeCliente.ToLower().Contains(searchLower) ||
                p.ClienteCpf.ToLower().Contains(searchLower)
            );
        }

        Pedidos = await query
            .OrderByDescending(p => p.DataPedido)
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