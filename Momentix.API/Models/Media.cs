namespace Momentix.API.Models
{
    public enum MediaType { Image, Video, Audio, Letter }

    public class Media
    {
        public int Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public MediaType Type { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public string UploadedById { get; set; } = string.Empty;
        public User UploadedBy { get; set; } = null!;

        public int? AlbumId { get; set; }
        public Album? Album { get; set; }

        public int? TimeCapsuleId { get; set; }
        public TimeCapsule? TimeCapsule { get; set; }
    }
}