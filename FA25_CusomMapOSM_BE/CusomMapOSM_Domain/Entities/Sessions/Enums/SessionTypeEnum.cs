namespace CusomMapOSM_Domain.Entities.Sessions.Enums;

/// <summary>
/// Types of session modes
/// </summary>
public enum SessionTypeEnum
{
    /// <summary>
    /// Teacher-controlled real-time session (like Kahoot)
    /// </summary>
    LIVE = 1,

    /// <summary>
    /// Students work at their own pace
    /// </summary>
    SELF_PACED = 2,

    /// <summary>
    /// Practice mode with no time limits
    /// </summary>
    PRACTICE = 3
}
