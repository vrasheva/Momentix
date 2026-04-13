namespace Momentix.API.DTOs
{
    public class TimeCapsuleResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime UnlockAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsUnlocked { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public int MediaCount { get; set; }
        public TimeSpan? TimeRemaining { get; set; }
    }
}