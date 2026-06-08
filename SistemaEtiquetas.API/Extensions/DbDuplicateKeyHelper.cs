using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace SistemaEtiquetas.API.Extensions;

internal static class DbDuplicateKeyHelper
{
    /// <summary>
    /// Postgres unique_violation (23505), ex.: índice único em PedidoExternoId.
    /// </summary>
    private const string UniqueViolation = "23505";

    public static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg && pg.SqlState == UniqueViolation;
}
