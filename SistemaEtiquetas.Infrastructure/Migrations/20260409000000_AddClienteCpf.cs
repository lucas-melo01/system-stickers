using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEtiquetas.Infrastructure.Migrations
{
    public partial class AddClienteCpf : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClienteCpf",
                table: "Pedidos",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClienteCpf",
                table: "Pedidos");
        }
    }
}
