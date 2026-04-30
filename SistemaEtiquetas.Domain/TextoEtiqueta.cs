using System.Globalization;
using System.Text;

namespace SistemaEtiquetas.Domain
{
    /// <summary>
    /// Normalização para impressão ZPL: impressoras em modo ASCII interpretam UTF-8
    /// incorrectamente (acentos viram caracteres estranhos). Remove combinações
    /// não-ASCII devolvendo texto compatível com a fonte padrão (^CF0).
    /// </summary>
    public static class TextoEtiqueta
    {
        public static string RemoverAcentos(string? texto)
        {
            if (string.IsNullOrEmpty(texto)) return texto ?? string.Empty;

            var sb = new StringBuilder(texto.Length);
            foreach (var c in texto.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
