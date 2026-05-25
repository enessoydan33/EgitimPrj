using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace EgitimPrj.Controllers
{
    public abstract class ProxyControllerBase : Controller
    {
        protected void AttachAuth(HttpRequestMessage requestMessage, string tokenSessionKey = "Token")
        {
            var token = BearerTokenNormalizer.Normalize(HttpContext.Session.GetString(tokenSessionKey));
            if (!string.IsNullOrWhiteSpace(token))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Ngrok tarayıcı uyarısını atlamak için gerekli header
            requestMessage.Headers.Add("ngrok-skip-browser-warning", "true");
        }

        /// <summary>
        /// Proxy çağrısından önce oturumda normalize edilmiş Bearer token olduğunu doğrular.
        /// </summary>
        protected IActionResult? RequireSessionBearer(string tokenSessionKey, string? userMessage = null)
        {
            if (!string.IsNullOrWhiteSpace(BearerTokenNormalizer.Normalize(HttpContext.Session.GetString(tokenSessionKey))))
                return null;

            var message = userMessage
                ?? "Oturumda yetkilendirme bilgisi yok. Lütfen tekrar giriş yapın.";
            return Unauthorized(new { message });
        }

        protected async Task<IActionResult> SendProxyAsync(HttpClient httpClient, HttpRequestMessage request)
        {
            try
            {
                var response = await httpClient.SendAsync(request);
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "text/plain";
                var content = await response.Content.ReadAsStringAsync();

                // JSON cevabı parse etmeye zorlamadan aynen geçiriyoruz.
                if (contentType.Contains("application/json"))
                {
                    return new ContentResult
                    {
                        StatusCode = (int)response.StatusCode,
                        ContentType = "application/json",
                        Content = content
                    };
                }

                if (response.IsSuccessStatusCode)
                {
                    return Content(content, contentType);
                }

                return StatusCode((int)response.StatusCode, new { message = content });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new { message = $"API hatası: {ex.Message}" });
            }
        }
    }

    /// <summary>
    /// API login cevaplarındaki token alan adları ve "Bearer " öneki farklılıklarını tek biçimde ele alır.
    /// </summary>
    public static class BearerTokenNormalizer
    {
        private static readonly string[] TokenPropertyNames =
        {
            "token", "accessToken", "access_token", "authToken", "jwt",
        };

        private static readonly string[] NestedObjectNames =
        {
            "data", "result", "payload",
        };

        public static string? Normalize(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            token = token.Trim().Trim('"');
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                token = token[7..].Trim();

            return string.IsNullOrWhiteSpace(token) ? null : token;
        }

        /// <summary>
        /// Login JSON gövdesinden (kök veya data/result içi) taşıyıcı token arar.
        /// </summary>
        public static string? FindInLoginJson(JsonElement root, int depth = 0)
        {
            if (depth > 5)
                return null;

            foreach (var name in TokenPropertyNames)
            {
                if (!TryGetPropertyInsensitive(root, name, out var el))
                    continue;
                if (el.ValueKind == JsonValueKind.String)
                {
                    var s = el.GetString();
                    if (!string.IsNullOrWhiteSpace(s))
                        return s;
                }
            }

            foreach (var container in NestedObjectNames)
            {
                if (!TryGetPropertyInsensitive(root, container, out var el))
                    continue;
                if (el.ValueKind == JsonValueKind.Object)
                {
                    var inner = FindInLoginJson(el, depth + 1);
                    if (!string.IsNullOrWhiteSpace(inner))
                        return inner;
                }
            }

            return null;
        }

        private static bool TryGetPropertyInsensitive(JsonElement el, string name, out JsonElement value)
        {
            foreach (var p in el.EnumerateObject())
            {
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = p.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}
