using System.Text.Json.Serialization;

namespace PinoyPantry.API.Services
{
    // Verifies Google reCAPTCHA v2 ("I'm not a robot") tokens server-side. The checkbox on the
    // website alone stops nothing — a bot can call the API directly — so every public form
    // endpoint must check the token here before doing anything. Fails closed: a missing secret,
    // a network error, or any non-success answer from Google all count as "not verified".
    public class RecaptchaService : IRecaptchaService
    {
        private const string VerifyUrl = "https://www.google.com/recaptcha/api/siteverify";

        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly ILogger<RecaptchaService> _logger;

        public RecaptchaService(HttpClient http, IConfiguration config, ILogger<RecaptchaService> logger)
        {
            _http = http;
            _config = config;
            _logger = logger;
        }

        public async Task<bool> VerifyAsync(string? token, string? remoteIp)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var secret = _config["Recaptcha:SecretKey"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                _logger.LogError("Recaptcha:SecretKey is not configured — rejecting all reCAPTCHA checks.");
                return false;
            }

            var form = new Dictionary<string, string> { ["secret"] = secret, ["response"] = token };
            if (!string.IsNullOrWhiteSpace(remoteIp))
                form["remoteip"] = remoteIp;

            try
            {
                using var res = await _http.PostAsync(VerifyUrl, new FormUrlEncodedContent(form));
                var result = await res.Content.ReadFromJsonAsync<SiteVerifyResponse>();

                if (result?.Success == true)
                    return true;

                _logger.LogWarning("reCAPTCHA rejected: {Errors}", string.Join(", ", result?.ErrorCodes ?? Array.Empty<string>()));
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "reCAPTCHA verification request failed.");
                return false;
            }
        }

        private class SiteVerifyResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("error-codes")]
            public string[]? ErrorCodes { get; set; }
        }
    }
}
