namespace CusomMapOSM_Application.Interfaces.Services.Osm;

public interface IOsmQueryService
{
    Task<string> QueryBBox(double south, double west, double north, double east);
    Task<string> QueryPoints(IEnumerable<(double lat, double lon)> points);
}
