using System.Text.Json;
using CusomMapOSM_API.Interfaces;
using CusomMapOSM_Application.Interfaces.Services.Osm;
using Microsoft.AspNetCore.Mvc;

namespace CusomMapOSM_API.Endpoints;

public class OsmQueryEndpoint : IEndpoint
{
    private const string API_PREFIX = "osm";

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(API_PREFIX)
            .WithTags(Tags.Osm);

        group.MapGet("/bbox", async (
            [FromQuery] double south,
            [FromQuery] double west,
            [FromQuery] double north,
            [FromQuery] double east,
            [FromServices] IOsmQueryService osmService) =>
        {
            try
            {
                var json = await osmService.QueryBBox(south, west, north, east);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("error", out var err) ||
                    doc.RootElement.TryGetProperty("remark", out var remark))
                {
                    var message = err.ValueKind == JsonValueKind.String ? err.GetString() : remark.GetString();
                    return Results.Problem(message);
                }

                var obj = JsonSerializer.Deserialize<object>(json);
                return Results.Json(obj);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        })
        .WithName("OsmQueryBBox")
        .WithDescription("Query OSM data by bounding box");

        group.MapGet("/points", async (
            [FromQuery] List<string> points,
            [FromServices] IOsmQueryService osmService) =>
        {
            if (points is null || points.Count == 0)
                return Results.BadRequest("points parameter is required");

            var coords = new List<(double lat, double lon)>();
            foreach (var p in points)
            {
                var parts = p.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2 ||
                    !double.TryParse(parts[0], out var lat) ||
                    !double.TryParse(parts[1], out var lon))
                {
                    return Results.BadRequest($"Invalid point format: {p}");
                }
                coords.Add((lat, lon));
            }

            try
            {
                var json = await osmService.QueryPoints(coords);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("error", out var err) ||
                    doc.RootElement.TryGetProperty("remark", out var remark))
                {
                    var message = err.ValueKind == JsonValueKind.String ? err.GetString() : remark.GetString();
                    return Results.Problem(message);
                }

                var obj = JsonSerializer.Deserialize<object>(json);
                return Results.Json(obj);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        })
        .WithName("OsmQueryPoints")
        .WithDescription("Query OSM data by list of points");
    }
}
