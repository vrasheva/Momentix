using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Momentix.Data.DTOs;

public class CreateAlbumDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreateAlbumWithDetailsDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<AlbumMemberInviteDto> Members { get; set; } = new();
    public string? LetterText { get; set; }
    public DateTime? LetterUnlockAt { get; set; }
}

public class AlbumMemberInviteDto
{
    public string UserId { get; set; } = string.Empty;
    public bool CanUpload { get; set; }
}