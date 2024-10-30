using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System;

namespace WeatherApi.Controllers
{
    [ApiController]
    [Route("api/weather")]
    public class WeatherController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public WeatherController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpPost("get-lat-lon")]
        public async Task<IActionResult> GetLatLon([FromBody] LocationRequest request)
        {
            try
            {
                Console.WriteLine($"Þehir: {request.City}, Ülke: {request.Country}");


                var geoUrl = $"https://nominatim.openstreetmap.org/search?q={request.City},{request.Country}&format=json&limit=1";
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, geoUrl);
                requestMessage.Headers.Add("User-Agent", "WeatherApp/1.0");
                var geoResponseMessage = await _httpClient.SendAsync(requestMessage);
                geoResponseMessage.EnsureSuccessStatusCode();
                var geoResponse = await geoResponseMessage.Content.ReadAsStringAsync();
                Console.WriteLine($"Geo Response Data: {geoResponse}");

                var geoJson = JsonDocument.Parse(geoResponse);

                if (geoJson.RootElement.GetArrayLength() == 0)
                {
                    Console.WriteLine("Enlem ve boylam bulunamadý.");
                    return BadRequest(new { message = "Geçerli bir þehir ve ülke adý girin." });
                }

                var lat = geoJson.RootElement[0].GetProperty("lat").GetString();
                var lon = geoJson.RootElement[0].GetProperty("lon").GetString();

                Console.WriteLine($"Enlem: {lat}, Boylam: {lon}");
                return Ok(new { Latitude = lat, Longitude = lon });
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"HttpRequestException: {e.Message}");
                return BadRequest(new { message = "Enlem ve boylam bilgisi alýnamadý.", error = e.Message });
            }
        }

        [HttpPost("get-weather")]
        public async Task<IActionResult> GetWeather([FromBody] LatLonRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Latitude) || string.IsNullOrEmpty(request.Longitude))
                {
                    Console.WriteLine("Latitude veya Longitude deðeri eksik.");
                    return BadRequest(new { message = "Latitude ve Longitude deðerleri gereklidir." });
                }


                var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={request.Latitude}&longitude={request.Longitude}&current_weather=true&temperature_unit={(request.Unit == "C" ? "celsius" : "fahrenheit")}";
                var weatherRequestMessage = new HttpRequestMessage(HttpMethod.Get, weatherUrl);
                weatherRequestMessage.Headers.Add("User-Agent", "WeatherApp/1.0");
                var weatherResponseMessage = await _httpClient.SendAsync(weatherRequestMessage);
                weatherResponseMessage.EnsureSuccessStatusCode();
                var weatherResponse = await weatherResponseMessage.Content.ReadAsStringAsync();
                Console.WriteLine($"Weather Response Data: {weatherResponse}");

                var weatherJson = JsonDocument.Parse(weatherResponse);

                return Ok(weatherJson.RootElement.GetProperty("current_weather"));
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"HttpRequestException: {e.Message}");
                return BadRequest(new { message = "Hava durumu bilgisi alýnamadý.", error = e.Message });
            }
        }

        public class LocationRequest
        {
            public string City { get; set; }
            public string Country { get; set; }
        }

        public class LatLonRequest
        {
            public string Latitude { get; set; }
            public string Longitude { get; set; }
            public string Unit { get; set; }
        }
    }
}
