using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace carrentalmvc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GeocodingController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GeocodingController> _logger;

        public GeocodingController(IHttpClientFactory httpClientFactory, ILogger<GeocodingController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpGet("reverse")]
        public async Task<IActionResult> ReverseGeocode(double lat, double lng)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "CarRentalApp/1.0");

                var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={lat}&lon={lng}&accept-language=vi&zoom=18";
                
                var response = await httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<JsonElement>(content);
                    
                    return Ok(new
                    {
                        display_name = data.GetProperty("display_name").GetString(),
                        lat = lat,
                        lon = lng
                    });
                }

                return Ok(new
                {
                    display_name = $"Vị trí {lat:F4}, {lng:F4}",
                    lat = lat,
                    lon = lng
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in reverse geocoding");
                return Ok(new
                {
                    display_name = $"Vị trí {lat:F4}, {lng:F4}",
                    lat = lat,
                    lon = lng
                });
            }
        }
    }
}