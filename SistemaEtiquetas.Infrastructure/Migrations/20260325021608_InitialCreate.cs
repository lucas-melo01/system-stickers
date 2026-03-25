using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEtiquetas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pedidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PedidoExternoId = table.Column<string>(type: "TEXT", nullable: false),
                    FornecedorId = table.Column<string>(type: "TEXT", nullable: false),
                    NomeCliente = table.Column<string>(type: "TEXT", nullable: false),
                    Produto = table.Column<string>(type: "TEXT", nullable: false),
                    Cor = table.Column<string>(type: "TEXT", nullable: false),
                    Tamanho = table.Column<string>(type: "TEXT", nullable: false),
                    DataPedido = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Impresso = table.Column<bool>(type: "INTEGER", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedidos", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pedidos");
        }
    }
}
