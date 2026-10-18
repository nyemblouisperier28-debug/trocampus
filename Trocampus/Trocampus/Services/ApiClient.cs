using System.Net.Http;
using System.Net.Http.Json;
using Trocampus.Models;

namespace Trocampus.Services
{
    public class ApiClient
    {
        private readonly HttpClient _http;

        public ApiClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<HomeSummary> GetHomeSummaryAsync()
        {
            return await _http.GetFromJsonAsync<HomeSummary>("api/home/summary");
        }

        public async Task<List<ListingDto>> GetListingsAsync()
        {
            return await _http.GetFromJsonAsync<List<ListingDto>>("api/listings");
        }
    }
}
