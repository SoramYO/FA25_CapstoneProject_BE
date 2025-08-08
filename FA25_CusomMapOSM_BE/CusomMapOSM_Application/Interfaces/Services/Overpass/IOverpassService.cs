namespace CusomMapOSM_Application.Interfaces.Services.Overpass;

public interface IOverpassService
{
    Task<string> GetDataByBoundingBoxAsync(double south, double west, double north, double east);
    Task<string> GetDataByCoordinatesAsync(IEnumerable<(double lat, double lon)> points);
}
