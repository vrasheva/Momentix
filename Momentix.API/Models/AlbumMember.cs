namespace Momentix.API.Models
{
    public class AlbumMember
    {
        public int Id { get; set; }
        public int AlbumId { get; set; }
        public Album Album { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;

        public bool CanUpload { get; set; } = false;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}