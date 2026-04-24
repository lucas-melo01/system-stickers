using System.Text.Json;
using System.Text.Json.Serialization;

namespace SistemaEtiquetas.UI.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiService> _logger;

        public ApiService(IHttpClientFactory httpClientFactory, ILogger<ApiService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("API");
            _logger = logger;
        }

        /// <summary>
        /// Obtém a configuração da API (impressora, tokens, etc)
        /// </summary>
        public async Task<dynamic> GetConfigAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/config");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<dynamic>(content, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    });
                }
                
                _logger.LogError($"Erro ao obter configuração. Status: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao chamar API de configuração: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Salva a configuração da API
        /// </summary>
        public async Task<bool> SaveConfigAsync(object config)
        {
            try
            {
                var json = JsonSerializer.Serialize(config);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/config", content);
                
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                
                _logger.LogError($"Erro ao salvar configuração. Status: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao chamar API de save config: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Verifica saúde da API
        /// </summary>
        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/health");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao fazer health check: {ex.Message}");
                return false;
            }
        }
    }
}
