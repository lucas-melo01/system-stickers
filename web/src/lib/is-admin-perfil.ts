/**
 * Alinha com o enum UsuarioPerfil da API: Admin = 1, Operador = 0.
 * Aceita o nome "Admin" ou o valor numérico (algumas respostas JSON podem serializar o perfil como número).
 */
export function isAdminPerfil(perfil: unknown): boolean {
  if (perfil == null) return false;
  if (typeof perfil === "number") return perfil === 1;
  if (typeof perfil === "string") {
    const t = perfil.trim().toLowerCase();
    if (t === "admin" || t === "1") return true;
  }
  return false;
}
