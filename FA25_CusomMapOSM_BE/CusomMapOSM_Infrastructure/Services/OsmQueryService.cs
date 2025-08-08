using System.Text;
using System.Linq;
using CusomMapOSM_Application.Interfaces.Services.Osm;

namespace CusomMapOSM_Infrastructure.Services;

public class OsmQueryService : IOsmQueryService
{
    private readonly HttpClient _httpClient;
    private const string OVERPASS_API = "https://overpass-api.de/api/interpreter";

    public OsmQueryService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> QueryBBox(double south, double west, double north, double east)
    {
        var query = $"[out:json];node({south},{west},{north},{east});out;";
        var content = new StringContent(query, Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = await _httpClient.PostAsync(OVERPASS_API, content);
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> QueryPoints(IEnumerable<(double lat, double lon)> points)
    {
        var poly = string.Join(" ", points.Select(p => $"{p.lat} {p.lon}"));
        var query = $"[out:json];node(poly:\"{poly}\");out;";
        var content = new StringContent(query, Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = await _httpClient.PostAsync(OVERPASS_API, content);
        return await response.Content.ReadAsStringAsync();
    }
}
