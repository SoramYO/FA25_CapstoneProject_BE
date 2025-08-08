using CusomMapOSM_Application.Common.Errors;
using CusomMapOSM_Application.Interfaces.Features.Osm;
using CusomMapOSM_Application.Models.DTOs.Features.Osm;
using Newtonsoft.Json;
using Optional;
using System.Collections.Generic;
using System.Net.Http;

namespace CusomMapOSM_Infrastructure.Features.Osm;

public class OsmQueryService : IOsmQueryService
{
    private readonly HttpClient _httpClient;
    private const string OVERPASS_API = "https://overpass-api.de/api/interpreter";

    public OsmQueryService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Option<OsmQueryResDto, Error>> QueryByBoundingBox(double south, double west, double north, double east, string filter)
    {
        var query = BuildBoundingBoxQuery(south, west, north, east, filter);
        return await ExecuteQuery(query);
    }

    public async Task<Option<OsmQueryResDto, Error>> QueryByCoordinates(double latitude, double longitude, double radius, string filter)
    {
        var query = BuildCoordinateQuery(latitude, longitude, radius, filter);
        return await ExecuteQuery(query);
    }

    private async Task<Option<OsmQueryResDto, Error>> ExecuteQuery(string query)
    {
        try
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("data", query)
            });

            var response = await _httpClient.PostAsync(OVERPASS_API, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<OsmQueryResDto>(json);
            if (result is null)
                return Option.None<OsmQueryResDto, Error>(Error.Failure("OsmQuery.Empty", "Empty response from Overpass API"));

            return Option.Some<OsmQueryResDto, Error>(result);
        }
        catch (Exception ex)
        {
            return Option.None<OsmQueryResDto, Error>(Error.Problem("OsmQuery.Error", ex.Message));
        }
    }

    private static string BuildBoundingBoxQuery(double south, double west, double north, double east, string filter)
    {
        return $"[out:json];{filter}({south},{west},{north},{east});out body;";
    }

    private static string BuildCoordinateQuery(double lat, double lon, double radius, string filter)
    {
        return $"[out:json];{filter}(around:{radius},{lat},{lon});out body;";
    }
}
