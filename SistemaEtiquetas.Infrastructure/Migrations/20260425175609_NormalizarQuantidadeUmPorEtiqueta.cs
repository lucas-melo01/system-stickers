using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEtiquetas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizarQuantidadeUmPorEtiqueta : Migration
    {
        // Normaliza dados existentes para a regra "1 PedidoItem = 1 etiqueta".
        //
        // Para cada PedidoItem com Quantidade > 1, gera Quantidade-1 cópias
        // pendentes (Impresso = false) com ValorCusto/ValorVenda = 0 — assim:
        //   - se a linha original já estava impressa, ela continua impressa
        //     e as cópias ficam pendentes para a operadora reimprimir só as
        //     unidades que faltam;
        //   - se estava pendente, todas continuam pendentes;
        //   - o total monetário do pedido é preservado (só a 1ª linha mantém
        //     ValorCusto/ValorVenda; restantes ficam zeradas).
        //
        // No fim, normaliza a coluna Quantidade para 1 em toda a tabela.
        // Operação é idempotente (rodar 2x não duplica nada).
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO ""PedidoItens""
                    (""PedidoId"", ""Produto"", ""SKU"", ""Cor"", ""Tamanho"",
                     ""Quantidade"", ""Impresso"", ""ValorCusto"", ""ValorVenda"")
                SELECT
                    pi.""PedidoId"",
                    pi.""Produto"",
                    pi.""SKU"",
                    pi.""Cor"",
                    pi.""Tamanho"",
                    1,
                    false,
                    0,
                    0
                FROM ""PedidoItens"" pi
                CROSS JOIN LATERAL generate_series(2, pi.""Quantidade"") AS gs
                WHERE pi.""Quantidade"" > 1;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""PedidoItens""
                SET ""Quantidade"" = 1
                WHERE ""Quantidade"" <> 1;
            ");
        }

        // Operação destrutiva: depois de explodir as linhas não há como
        // reconstruir a quantidade original a partir das cópias (perdemos a
        // associação 1↔N). Se for preciso reverter, é via backup do BD.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
