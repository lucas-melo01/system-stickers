using Microsoft.EntityFrameworkCore;
using SistemaEtiquetas.Domain.Entities;

namespace SistemaEtiquetas.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Pedido> Pedidos { get; set; }

        public DbSet<PedidoItem> PedidoItens { get; set; }

        public DbSet<Configuracao> Configuracoes { get; set; }

        public DbSet<UsuarioSistema> Usuarios { get; set; }

        public DbSet<Fornecedor> Fornecedores { get; set; }

        public DbSet<Produto> Produtos { get; set; }

        public DbSet<NotificacaoFornecedor> NotificacoesFornecedor { get; set; }

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
                b.HasIndex(p => p.PedidoExternoId).IsUnique();
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

            modelBuilder.Entity<UsuarioSistema>(b =>
            {
                b.HasKey(u => u.Id);
                b.Property(u => u.Email).IsRequired();
            });

            modelBuilder.Entity<Fornecedor>(b =>
            {
                b.HasKey(f => f.Id);
                b.Property(f => f.Id).ValueGeneratedOnAdd();
                b.Property(f => f.NomeRazaoSocial).IsRequired();
                b.Property(f => f.WhatsApp).IsRequired();
            });

            modelBuilder.Entity<Produto>(b =>
            {
                b.HasKey(p => p.Id);
                b.Property(p => p.Id).ValueGeneratedOnAdd();
                b.Property(p => p.Nome).IsRequired();
                b.HasIndex(p => new { p.Loja, p.ProdutoIdLojaIntegrada }).IsUnique();
                b.HasOne(p => p.Fornecedor)
                    .WithMany(f => f.Produtos)
                    .HasForeignKey(p => p.FornecedorId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<NotificacaoFornecedor>(b =>
            {
                b.HasKey(n => n.Id);
                b.Property(n => n.Id).ValueGeneratedOnAdd();
                b.Property(n => n.PedidoExternoId).IsRequired();
                b.Property(n => n.NomeCliente).IsRequired();
                b.Property(n => n.MensagemTexto).IsRequired();
                b.HasOne(n => n.Pedido)
                    .WithMany()
                    .HasForeignKey(n => n.PedidoId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(n => n.PedidoItem)
                    .WithMany()
                    .HasForeignKey(n => n.PedidoItemId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(n => n.Fornecedor)
                    .WithMany()
                    .HasForeignKey(n => n.FornecedorId)
                    .OnDelete(DeleteBehavior.SetNull);
                b.HasOne(n => n.Produto)
                    .WithMany()
                    .HasForeignKey(n => n.ProdutoId)
                    .OnDelete(DeleteBehavior.SetNull);
                b.HasIndex(n => n.CriadoEm);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
