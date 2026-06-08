using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEtiquetas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniquePedidoExternoId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_PedidoExternoId",
                table: "Pedidos",
                column: "PedidoExternoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pedidos_PedidoExternoId",
                table: "Pedidos");
        }
    }
}
