using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEtiquetas.Domain.Entities;

public class UsuarioSistema
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Nome { get; set; }

    public UsuarioPerfil Perfil { get; set; } = UsuarioPerfil.Operador;

    public bool Ativo { get; set; } = true;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

public enum UsuarioPerfil
{
    Operador = 0,
    Admin = 1
}
