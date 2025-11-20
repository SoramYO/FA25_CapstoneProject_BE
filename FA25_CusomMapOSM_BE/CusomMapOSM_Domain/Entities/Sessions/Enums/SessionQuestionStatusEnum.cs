namespace CusomMapOSM_Domain.Entities.Sessions.Enums;

/// <summary>
/// Status of a question within a session
/// </summary>
public enum SessionQuestionStatusEnum
{
    /// <summary>
    /// Question is waiting in queue
    /// </summary>
    QUEUED = 1,

    /// <summary>
    /// Question is currently being displayed/answered
    /// </summary>
    ACTIVE = 2,

    /// <summary>
    /// Question was skipped by teacher
    /// </summary>
    SKIPPED = 3,

    /// <summary>
    /// Question has been completed
    /// </summary>
    COMPLETED = 4
}
