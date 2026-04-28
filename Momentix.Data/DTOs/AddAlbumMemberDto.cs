using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Momentix.Data.DTOs;

public class AddAlbumMemberDto
{
    public string UserEmail { get; set; } = string.Empty;
    public bool CanUpload { get; set; }
}