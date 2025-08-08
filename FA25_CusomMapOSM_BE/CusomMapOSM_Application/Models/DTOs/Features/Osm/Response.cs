using System.Collections.Generic;

namespace CusomMapOSM_Application.Models.DTOs.Features.Osm;

public record OsmElementDto
{
    public required string Type { get; set; }
    public long Id { get; set; }
    public double? Lat { get; set; }
    public double? Lon { get; set; }
    public Dictionary<string, string>? Tags { get; set; }
}

public record OsmQueryResDto
{
    public required List<OsmElementDto> Elements { get; set; } = new();
}
