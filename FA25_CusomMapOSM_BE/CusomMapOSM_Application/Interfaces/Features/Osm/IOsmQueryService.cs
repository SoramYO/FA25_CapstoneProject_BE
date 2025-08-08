using CusomMapOSM_Application.Common.Errors;
using CusomMapOSM_Application.Models.DTOs.Features.Osm;
using Optional;

namespace CusomMapOSM_Application.Interfaces.Features.Osm;

public interface IOsmQueryService
{
    Task<Option<OsmQueryResDto, Error>> QueryByBoundingBox(double south, double west, double north, double east, string filter);
    Task<Option<OsmQueryResDto, Error>> QueryByCoordinates(double latitude, double longitude, double radius, string filter);
}
