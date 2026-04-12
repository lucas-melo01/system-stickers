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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Pedido>(b =>
            {
                b.HasKey(p => p.Id);
                b.Property(p => p.Id).ValueGeneratedOnAdd();

                b.Property(p => p.PedidoExternoId).IsRequired();
                b.Property(p => p.NomeCliente).IsRequired();

                b.HasMany(p => p.Itens)
                    .WithOne(i => i.Pedido)
                    .HasForeignKey(i => i.PedidoId)
                    .IsRequired();
            });

            modelBuilder.Entity<PedidoItem>(b =>
            {
                b.HasKey(i => i.Id);
                b.Property(i => i.Id).ValueGeneratedOnAdd();
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
