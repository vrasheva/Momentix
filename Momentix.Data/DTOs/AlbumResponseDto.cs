using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Momentix.Data.DTOs;

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