using System.Net.Http;
using System.Net.Http.Json;

namespace Trocampus.Services
{
    public class AuthService
    {
        private readonly HttpClient _http;

        public AuthService(HttpClient http)
        {
            _http = http;
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", new { email, password });
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RegisterAsync(string name, string email, string password)
        {
            var response = await _http.PostAsJsonAsync("api/auth/register", new { name, email, password });
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ForgotPasswordAsync(string email)
        {
            var response = await _http.PostAsJsonAsync("api/auth/forgot", new { email });
            return response.IsSuccessStatusCode;
        }
    }
}
