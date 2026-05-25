using Momentix.Data.Models;

public class MediaResponseDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? LetterText { get; set; }  // ← добави това
    public MediaType Type { get; set; }
    public DateTime UploadedAt { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
    public DateTime? UnlockAt { get; set; }
    public bool IsLocked => UnlockAt.HasValue && UnlockAt.Value > DateTime.UtcNow;
}