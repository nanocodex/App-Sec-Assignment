using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApplication1.Services
{
    public class ReCaptchaService : IReCaptchaService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ReCaptchaService> _logger;

        public ReCaptchaService(
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<ReCaptchaService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> VerifyTokenAsync(string token, string action)
        {
            try
            {
                // Check if token is provided
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("reCAPTCHA token is empty or null");
                    return false;
                }

                var secretKey = _configuration["ReCaptcha:SecretKey"];
                if (string.IsNullOrEmpty(secretKey))
                {
                    _logger.LogError("ReCaptcha SecretKey is not configured");
                    return false;
                }

                var requestData = new Dictionary<string, string>
                {
                    { "secret", secretKey },
                    { "response", token }
                };

                var response = await _httpClient.PostAsync(
                    "https://www.google.com/recaptcha/api/siteverify",
                    new FormUrlEncodedContent(requestData));

                var jsonResponse = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("reCAPTCHA API Response: {Response}", jsonResponse);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var result = JsonSerializer.Deserialize<ReCaptchaResponse>(jsonResponse, options);

                if (result == null)
                {
                    _logger.LogError("Failed to deserialize reCAPTCHA response");
                    return false;
                }

                // Log error codes if present
                if (result.ErrorCodes != null && result.ErrorCodes.Length > 0)
                {
                    _logger.LogWarning("reCAPTCHA returned error codes: {ErrorCodes}", string.Join(", ", result.ErrorCodes));
                }

                var threshold = _configuration.GetValue<double>("ReCaptcha:ScoreThreshold");
                if (threshold == 0)
                {
                    threshold = 0.5; // Default threshold
                }

                var isValid = result.Success && 
                              result.Score >= threshold && 
                              result.Action == action;

                if (!isValid)
                {
                    _logger.LogWarning(
                        "reCAPTCHA validation failed. Success: {Success}, Score: {Score}, Action: {Action}, Expected Action: {ExpectedAction}, Threshold: {Threshold}",
                        result.Success, result.Score, result.Action, action, threshold);
                }
                else
                {
                    _logger.LogInformation(
                        "reCAPTCHA validation successful. Score: {Score}, Action: {Action}",
                        result.Score, result.Action);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying reCAPTCHA token");
                return false;
            }
        }

        private class ReCaptchaResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }
            
            [JsonPropertyName("score")]
            public double Score { get; set; }
            
            [JsonPropertyName("action")]
            public string Action { get; set; } = string.Empty;
            
            [JsonPropertyName("challenge_ts")]
            public string ChallengeTs { get; set; } = string.Empty;
            
            [JsonPropertyName("hostname")]
            public string Hostname { get; set; } = string.Empty;
            
            [JsonPropertyName("error-codes")]
            public string[] ErrorCodes { get; set; } = Array.Empty<string>();
        }
    }
}
