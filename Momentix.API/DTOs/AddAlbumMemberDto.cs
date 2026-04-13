namespace Momentix.API.DTOs
{
    public class AddAlbumMemberDto
    {
        public string UserEmail { get; set; } = string.Empty;
        public bool CanUpload { get; set; } = false;
    }
}