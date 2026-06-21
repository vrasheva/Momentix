namespace Momentix.Data.DTOs;

public class AlbumResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public int MediaCount { get; set; }
    public bool IsOwner { get; set; }
    public bool IsSharedWithMe { get; set; }
    public string Color { get; set; } = "#EEEDFE";
    public List<string> ThumbnailUrls { get; set; } = new();

    public string? Thumbnail0 => ThumbnailUrls.Count > 0 ? ThumbnailUrls[0] : null;
    public string? Thumbnail1 => ThumbnailUrls.Count > 1 ? ThumbnailUrls[1] : null;
    public string? Thumbnail2 => ThumbnailUrls.Count > 2 ? ThumbnailUrls[2] : null;
    public string? Thumbnail3 => ThumbnailUrls.Count > 3 ? ThumbnailUrls[3] : null;

    public bool HasPhoto0 => ThumbnailUrls.Count > 0;
    public bool HasPhoto1 => ThumbnailUrls.Count > 1;
    public bool HasPhoto2 => ThumbnailUrls.Count > 2;
    public bool HasPhoto3 => ThumbnailUrls.Count > 3;
}