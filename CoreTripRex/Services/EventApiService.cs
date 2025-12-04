using System.Text.Json;
using CoreTripRex.Models.EventAPI;
namespace CoreTripRex.Services
{
    public class EventApiService
    {
        private readonly HttpClient _http;

        public EventApiService(HttpClient http)
        {
            _http = http;
            _http.BaseAddress = new Uri("https://cis-iis2.temple.edu/Fall2025/CIS3342_bweitzel/WebAPI/api/events");
        }

        public async Task<List<Activity>> GetActivities(string city, string state)
        {
            var resp = await _http.GetAsync($"events/GetActivities?city={city}&state={state}");
            resp.EnsureSuccessStatusCode();

            string json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Activity>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }

}
