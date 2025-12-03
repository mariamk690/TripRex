using CoreTripRex.Models.CarAPI;
using System.Text;
using System.Text.Json;

namespace CoreTripRex.Services
{
    public class CarApiService
    {
        private readonly HttpClient _http;

        public CarApiService(HttpClient http)
        {
            _http = http;
            _http.BaseAddress = new Uri("https://cis-iis2.temple.edu/Fall2025/CIS3342_tur38680/WebAPI/");
        }

        public async Task<List<Agency>> GetAgencies(string city, string state)
        {
            var resp = await _http.GetAsync($"api/RentalCar/agencies?city={city}&state={state}");
            resp.EnsureSuccessStatusCode();

            string json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Agency>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<List<Car>> GetCars(int agencyId, string city, string state)
        {
            var resp = await _http.GetAsync(
                $"api/RentalCar/carsbyagency?agencyID={agencyId}&city={city}&state={state}"
            );

            resp.EnsureSuccessStatusCode();

            string json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Car>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }


}
