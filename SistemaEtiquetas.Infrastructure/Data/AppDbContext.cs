using Microsoft.EntityFrameworkCore;
using SistemaEtiquetas.Domain.Entities;

namespace SistemaEtiquetas.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Pedido> Pedidos { get; set; }

        public DbSet<PedidoItem> PedidoItens { get; set; }

        public DbSet<Configuracao> Configuracoes { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
    }
}
