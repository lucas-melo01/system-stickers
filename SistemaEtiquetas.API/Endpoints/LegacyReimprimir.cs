using Microsoft.EntityFrameworkCore;
using SistemaEtiquetas.API.Services;
using SistemaEtiquetas.Infrastructure.Data;

namespace SistemaEtiquetas.API.Endpoints;

public static class LegacyReimprimir
{
    public static async Task<IResult> RetornarZpl(int itemId, AppDbContext db)
    {
        var item = await db.PedidoItens.AsNoTracking().Include(i => i.Pedido).FirstOrDefaultAsync(i => i.Id == itemId);
        if (item == null) return Results.NotFound();
        var zpl = new EtiquetaService().GerarZpl(item.Pedido, item);
        return Results.Json(new
        {
            message = "Impressão local: use GET /api/pedido-itens/{id}/zpl ou o JSON abaixo.",
            zpl
        });
    }
}
