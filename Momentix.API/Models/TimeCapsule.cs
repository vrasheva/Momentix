namespace Momentix.API.Models
{
    public class TimeCapsule
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime UnlockAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsUnlocked { get; set; } = false;

        public string OwnerId { get; set; } = string.Empty;
        public User Owner { get; set; } = null!;

        public ICollection<Media> MediaItems { get; set; } = new List<Media>();
        public ICollection<TimeCapsuleMember> Members { get; set; } = new List<TimeCapsuleMember>();
    }
}