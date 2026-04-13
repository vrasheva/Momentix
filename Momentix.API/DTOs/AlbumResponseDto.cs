namespace Momentix.API.DTOs
{
    public class AlbumResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public int MediaCount { get; set; }
    }
}