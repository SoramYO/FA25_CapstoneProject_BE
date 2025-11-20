namespace CusomMapOSM_Domain.Entities.QuestionBanks;

/// <summary>
/// Answer options for multiple choice and true/false questions
/// </summary>
public class QuestionOption
{
    public Guid QuestionOptionId { get; set; }
    public Guid QuestionId { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public string? OptionImageUrl { get; set; }
    public bool IsCorrect { get; set; } = false;
    public int DisplayOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Question? Question { get; set; }
}
