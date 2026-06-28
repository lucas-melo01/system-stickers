using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SistemaEtiquetas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCadastrosNotificacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Fornecedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NomeRazaoSocial = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    WhatsApp = table.Column<string>(type: "text", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fornecedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Produtos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Loja = table.Column<int>(type: "integer", nullable: false),
                    ProdutoIdLojaIntegrada = table.Column<long>(type: "bigint", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Sku = table.Column<string>(type: "text", nullable: true),
                    CodigoFornecedor = table.Column<string>(type: "text", nullable: true),
                    FornecedorId = table.Column<int>(type: "integer", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produtos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Produtos_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "NotificacoesFornecedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PedidoId = table.Column<int>(type: "integer", nullable: false),
                    PedidoItemId = table.Column<int>(type: "integer", nullable: false),
                    FornecedorId = table.Column<int>(type: "integer", nullable: true),
                    ProdutoId = table.Column<int>(type: "integer", nullable: true),
                    Loja = table.Column<int>(type: "integer", nullable: false),
                    PedidoExternoId = table.Column<string>(type: "text", nullable: false),
                    NomeCliente = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MensagemTexto = table.Column<string>(type: "text", nullable: false),
                    WhatsAppMessageId = table.Column<string>(type: "text", nullable: true),
                    Erro = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EnviadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificacoesFornecedor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificacoesFornecedor_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NotificacoesFornecedor_PedidoItens_PedidoItemId",
                        column: x => x.PedidoItemId,
                        principalTable: "PedidoItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificacoesFornecedor_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificacoesFornecedor_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificacoesFornecedor_CriadoEm",
                table: "NotificacoesFornecedor",
                column: "CriadoEm");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacoesFornecedor_FornecedorId",
                table: "NotificacoesFornecedor",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacoesFornecedor_PedidoId",
                table: "NotificacoesFornecedor",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacoesFornecedor_PedidoItemId",
                table: "NotificacoesFornecedor",
                column: "PedidoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacoesFornecedor_ProdutoId",
                table: "NotificacoesFornecedor",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_FornecedorId",
                table: "Produtos",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_Loja_ProdutoIdLojaIntegrada",
                table: "Produtos",
                columns: new[] { "Loja", "ProdutoIdLojaIntegrada" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "NotificacoesFornecedor");
            migrationBuilder.DropTable(name: "Produtos");
            migrationBuilder.DropTable(name: "Fornecedores");
        }
    }
}
