namespace Momentix.API.DTOs
{
    public class CreateAlbumDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}