using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BorsaBot.Models;

namespace BorsaBot.Core
{
    public class ApiClient : IDisposable
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private static readonly JsonSerializerOptions _jsonOpt = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        public ApiClient(string baseUrl)
        {
            _baseUrl = baseUrl;
            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(3)
            };
        }

        public async Task<TradeSignal?> AnalizEt(MarketItem item)
        {
            var payload = new
            {
                item_adi = item.ItemAdi,
                fiyat = item.Fiyat,
                pazar_id = item.PazarId,
                satici_id = item.SaticiId,
                miktar = item.Miktar
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload, _jsonOpt),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _http.PostAsync($"{_baseUrl}/analiz_et", content);
            if (!response.IsSuccessStatusCode) return null;

            var body = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TradeSignal>(body, _jsonOpt);
        }

        public async Task<bool> SunucuAktifMi()
        {
            try
            {
                var response = await _http.GetAsync($"{_baseUrl}/saglik");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<List<Dictionary<string, object>>> PiyasaListesiAl()
        {
            try
            {
                var response = await _http.GetAsync($"{_baseUrl}/piyasa");
                var body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Dictionary<string, object>>>(body, _jsonOpt)
                       ?? new List<Dictionary<string, object>>();
            }
            catch { return new List<Dictionary<string, object>>(); }
        }

        public void Dispose() => _http.Dispose();
    }
}