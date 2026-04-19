using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEtiquetas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJsonWebhookColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "jsonWebhook",
                table: "Pedidos",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "jsonWebhook",
                table: "Pedidos");
        }
    }
}
