namespace SistemaEtiquetas.API.Services;

public class WhatsAppApiOptions
{
    public string PhoneNumberId { get; set; } = "";
    public string AccessToken { get; set; } = "";
    public string TemplateName { get; set; } = "";
    public string TemplateLanguage { get; set; } = "pt_BR";
    public string GraphApiVersion { get; set; } = "v21.0";
}
