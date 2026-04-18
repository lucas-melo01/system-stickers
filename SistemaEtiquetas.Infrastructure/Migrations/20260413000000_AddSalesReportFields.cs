using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEtiquetas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesReportFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Adicionar campos à tabela Pedidos
            migrationBuilder.AddColumn<string>(
                name: "Vendedor",
                table: "Pedidos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoEnvio",
                table: "Pedidos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormaPagamento",
                table: "Pedidos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorFrete",
                table: "Pedidos",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            // Adicionar campos à tabela PedidoItens
            migrationBuilder.AddColumn<decimal>(
                name: "ValorCusto",
                table: "PedidoItens",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorVenda",
                table: "PedidoItens",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remover campos da tabela PedidoItens
            migrationBuilder.DropColumn(
                name: "ValorCusto",
                table: "PedidoItens");

            migrationBuilder.DropColumn(
                name: "ValorVenda",
                table: "PedidoItens");

            // Remover campos da tabela Pedidos
            migrationBuilder.DropColumn(
                name: "Vendedor",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "TipoEnvio",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "FormaPagamento",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "ValorFrete",
                table: "Pedidos");
        }
    }
}
