using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore; 
using SistemaEtiquetas.Domain.Entities;
using SistemaEtiquetas.Infrastructure.Data;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    private readonly AppDbContext _db;

    public List<PedidoItem> Itens { get; set; }

    public ErrorModel(AppDbContext db)
    {
        _db = db;
    }

    public async Task OnGetAsync() // opcional: renomear para OnGetAsync
    {
        Itens = await _db.PedidoItens
            .Where(i => i.Erro)
            .ToListAsync();
    }
}