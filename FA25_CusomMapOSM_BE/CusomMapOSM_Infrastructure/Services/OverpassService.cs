using System.Net.Http;
using System.Text;
using System.Linq;
using CusomMapOSM_Application.Interfaces.Services.Cache;
using CusomMapOSM_Application.Interfaces.Services.Overpass;
using Microsoft.Extensions.Logging;

namespace CusomMapOSM_Infrastructure.Services;

public class OverpassService : IOverpassService
{
    private readonly HttpClient _httpClient;
    private readonly IRedisCacheService _cacheService;
    private readonly ILogger<OverpassService> _logger;

    private static int _cacheHitCount;
    private static int _cacheMissCount;

    public OverpassService(HttpClient httpClient, IRedisCacheService cacheService, ILogger<OverpassService> logger)
    {
        _httpClient = httpClient;
        _cacheService = cacheService;
        _logger = logger;
        _httpClient.BaseAddress ??= new Uri("https://overpass-api.de/api/");
    }

    public async Task<string> GetDataByBoundingBoxAsync(double south, double west, double north, double east)
    {
        var cacheKey = $"bbox:{south}:{west}:{north}:{east}";
        var cached = await _cacheService.Get<string>(cacheKey);
        if (!string.IsNullOrEmpty(cached))
        {
            _cacheHitCount++;
            _logger.LogInformation("Overpass cache hit for {Key}. HitCount: {HitCount}", cacheKey, _cacheHitCount);
            return cached;
        }

        _cacheMissCount++;
        _logger.LogInformation("Overpass cache miss for {Key}. MissCount: {MissCount}", cacheKey, _cacheMissCount);

        var query = $"[out:json];(node({south},{west},{north},{east});way({south},{west},{north},{east});relation({south},{west},{north},{east}););out body;>;out skel qt;";
        var response = await _httpClient.PostAsync("interpreter", new StringContent(query, Encoding.UTF8, "application/x-www-form-urlencoded"));
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        await _cacheService.Set(cacheKey, content, TimeSpan.FromHours(1));
        return content;
    }

    public async Task<string> GetDataByCoordinatesAsync(IEnumerable<(double lat, double lon)> points)
    {
        var pointKey = string.Join(";", points.Select(p => $"{p.lat},{p.lon}"));
        var cacheKey = $"points:{pointKey}";
        var cached = await _cacheService.Get<string>(cacheKey);
        if (!string.IsNullOrEmpty(cached))
        {
            _cacheHitCount++;
            _logger.LogInformation("Overpass cache hit for {Key}. HitCount: {HitCount}", cacheKey, _cacheHitCount);
            return cached;
        }

        _cacheMissCount++;
        _logger.LogInformation("Overpass cache miss for {Key}. MissCount: {MissCount}", cacheKey, _cacheMissCount);

        var coordsString = string.Join(" ", points.Select(p => $"{p.lat} {p.lon}"));
        var query = $"[out:json];(way(poly:\"{coordsString}\");>;);out;";
        var response = await _httpClient.PostAsync("interpreter", new StringContent(query, Encoding.UTF8, "application/x-www-form-urlencoded"));
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        await _cacheService.Set(cacheKey, content, TimeSpan.FromHours(1));
        return content;
    }
}
