using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SistemaEtiquetas.Domain.Entities;
using SistemaEtiquetas.Infrastructure.Data;

namespace SistemaEtiquetas.API.Services;

public static class AuthUsuarioHelper
{
    public static Guid? GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (sub != null && Guid.TryParse(sub, out var id))
            return id;
        return null;
    }

    public static async Task<UsuarioSistema?> GetOrCreateUsuarioAsync(
        AppDbContext db,
        ClaimsPrincipal principal,
        IConfiguration config,
        CancellationToken ct = default)
    {
        var id = GetUserId(principal);
        if (id == null) return null;

        var email = principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst("email")?.Value
            ?? "";

        var existing = await db.Usuarios.FindAsync(new object[] { id.Value }, ct);
        if (existing != null)
            return existing;

        var bootstrap = config["Auth:BootstrapAdminEmails"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? Array.Empty<string>();
        var isAdmin = bootstrap.Any(e => e.Equals(email, StringComparison.OrdinalIgnoreCase));

        var novo = new UsuarioSistema
        {
            Id = id.Value,
            Email = email,
            Nome = principal.FindFirst("name")?.Value,
            Perfil = isAdmin ? UsuarioPerfil.Admin : UsuarioPerfil.Operador,
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };
        db.Usuarios.Add(novo);
        await db.SaveChangesAsync(ct);
        return novo;
    }

    public static async Task<bool> IsAdminAsync(AppDbContext db, Guid userId, CancellationToken ct = default)
    {
        var u = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, ct);
        return u is { Ativo: true, Perfil: UsuarioPerfil.Admin };
    }
}
