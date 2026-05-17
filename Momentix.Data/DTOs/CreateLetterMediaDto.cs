namespace Momentix.Data.DTOs;

public class CreateLetterMediaDto
{
    public string Text { get; set; } = string.Empty;
    public DateTime? UnlockAt { get; set; }
}