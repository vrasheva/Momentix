namespace Momentix.API.DTOs
{
    public class CreateTimeCapsuleDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime UnlockAt { get; set; }
    }
}