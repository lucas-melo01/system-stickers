using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEtiquetas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjustarDataPedidoParaBrasilia : Migration
    {
        // Corrige o legado da rotina antiga do webhook que assumia UTC quando
        // o payload vinha sem zona (Kind=Unspecified) e portanto adiantava
        // 3h os Pedidos.DataPedido em relação a Brasília.
        //
        // Reinterpreta cada DataPedido "como se já estivesse em Brasília" e
        // devolve o instante UTC equivalente. O operador AT TIME ZONE faz
        // exactamente isso e respeita DST histórico (relevante para datas
        // anteriores a 2019, quando o Brasil ainda tinha horário de verão).
        //
        // Exemplo: valor 2026-04-25T14:30:00Z (que na prática correspondia
        // a 14:30 de Brasília erroneamente gravado como UTC) torna-se
        // 2026-04-25T17:30:00Z (14:30 em Brasília → 17:30 UTC).
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""Pedidos""
                SET ""DataPedido"" =
                    (""DataPedido"" AT TIME ZONE 'UTC') AT TIME ZONE 'America/Sao_Paulo';
            ");
        }

        // Operação destrutiva: depois do ajuste já não há marcador para
        // distinguir o que foi convertido. Reverter exigiria backup.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
