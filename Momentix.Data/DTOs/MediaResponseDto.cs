using Momentix.Data.Models;

namespace Momentix.Data.DTOs;

public class MediaResponseDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public MediaType Type { get; set; }
    public DateTime UploadedAt { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
    public DateTime? UnlockAt { get; set; }
    public bool IsLocked => UnlockAt.HasValue && UnlockAt.Value > DateTime.UtcNow;
}