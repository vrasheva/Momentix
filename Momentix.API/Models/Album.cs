namespace Momentix.API.Models
{
    public class Album
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string OwnerId { get; set; } = string.Empty;
        public User Owner { get; set; } = null!;

        public ICollection<Media> MediaItems { get; set; } = new List<Media>();
        public ICollection<AlbumMember> Members { get; set; } = new List<AlbumMember>();
    }
}