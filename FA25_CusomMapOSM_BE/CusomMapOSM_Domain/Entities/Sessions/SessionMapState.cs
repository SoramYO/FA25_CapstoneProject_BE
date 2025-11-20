using CusomMapOSM_Domain.Entities.Layers;
using CusomMapOSM_Domain.Entities.Locations;

namespace CusomMapOSM_Domain.Entities.Sessions;

/// <summary>
/// Session Map State - Real-time map synchronization for Teacher Focus feature
/// </summary>
public class SessionMapState
{
    public Guid SessionMapStateId { get; set; }
    public Guid SessionId { get; set; }
    public Guid? SessionQuestionId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal ZoomLevel { get; set; }
    public decimal Bearing { get; set; } = 0;
    public decimal Pitch { get; set; } = 0;
    public int TransitionDuration { get; set; } = 1000;
    public Guid? HighlightedLocationId { get; set; }
    public Guid? HighlightedLayerId { get; set; }
    public bool IsLocked { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Session? Session { get; set; }
    public SessionQuestion? SessionQuestion { get; set; }
    public Location? HighlightedLocation { get; set; }
    public Layer? HighlightedLayer { get; set; }
}
