using Microsoft.AspNetCore.Identity;

namespace Momentix.API.Models
{
    public class User : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int Streak { get; set; } = 0;

        public ICollection<Album> Albums { get; set; } = new List<Album>();
        public ICollection<AlbumMember> AlbumMemberships { get; set; } = new List<AlbumMember>();
        public ICollection<TimeCapsule> TimeCapsules { get; set; } = new List<TimeCapsule>();
    }
}