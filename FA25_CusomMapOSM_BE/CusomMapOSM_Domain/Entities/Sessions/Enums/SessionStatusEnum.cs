namespace CusomMapOSM_Domain.Entities.Sessions.Enums;

/// <summary>
/// Status of a session lifecycle
/// </summary>
public enum SessionStatusEnum
{
    /// <summary>
    /// Session is being created/edited
    /// </summary>
    DRAFT = 1,

    /// <summary>
    /// Waiting lobby - students are joining
    /// </summary>
    WAITING = 2,

    /// <summary>
    /// Session is actively running
    /// </summary>
    IN_PROGRESS = 3,

    /// <summary>
    /// Session is temporarily paused
    /// </summary>
    PAUSED = 4,

    /// <summary>
    /// Session has finished
    /// </summary>
    COMPLETED = 5,

    /// <summary>
    /// Session was cancelled
    /// </summary>
    CANCELLED = 6
}
