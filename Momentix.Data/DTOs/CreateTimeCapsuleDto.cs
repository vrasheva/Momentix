using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Momentix.Data.DTOs;

public class CreateTimeCapsuleDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime UnlockAt { get; set; }
}