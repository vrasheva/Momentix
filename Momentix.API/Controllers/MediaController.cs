using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Momentix.Data.Data;
using Momentix.Data.DTOs;
using Momentix.Data.Models;
using System.Security.Claims;

namespace Momentix.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MediaController : ControllerBase
{
    private readonly AppDbContext _context;

    public MediaController(AppDbContext context)
    {
        _context = context;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("album/{albumId}")]
    public async Task<IActionResult> GetAlbumMedia(int albumId)
    {
        var userId = GetUserId();
        var hasAccess = await _context.Albums
            .AnyAsync(a => a.Id == albumId && (a.OwnerId == userId || a.Members.Any(m => m.UserId == userId)));

        if (!hasAccess)
            return Forbid();

        var media = await _context.MediaItems
            .Where(m => m.AlbumId == albumId)
            .Include(m => m.UploadedBy)
            .OrderByDescending(m => m.UploadedAt)
            .Select(m => new MediaResponseDto
            {
                Id = m.Id,
                Url = m.Url,
                LetterText = m.Type == MediaType.Letter ? m.Url : null,  // ← добави
                Type = m.Type,
                UploadedAt = m.UploadedAt,
                UploadedById = m.UploadedById,
                UploadedByName = m.UploadedBy.FullName,
                UnlockAt = m.UnlockAt
            })
            .ToListAsync();

        return Ok(media);
    }

    [HttpGet("timecapsule/{timeCapsuleId}")]
    public async Task<IActionResult> GetTimeCapsuleMedia(int timeCapsuleId)
    {
        var userId = GetUserId();
        var now = DateTime.UtcNow;
        var capsule = await _context.TimeCapsules
            .Include(tc => tc.Members)
            .FirstOrDefaultAsync(tc => tc.Id == timeCapsuleId);

        if (capsule == null)
            return NotFound("Capsule was not found.");

        var hasAccess = capsule.OwnerId == userId ||
                        capsule.Members.Any(m => m.UserId == userId);

        if (!hasAccess)
            return Forbid();

        if (!capsule.IsUnlocked && capsule.UnlockAt > now)
            return BadRequest("Capsule is still locked.");

        var media = await _context.MediaItems
            .Where(m => m.TimeCapsuleId == timeCapsuleId)
            .Include(m => m.UploadedBy)
            .OrderByDescending(m => m.UploadedAt)
            .Select(m => new MediaResponseDto
            {
                Id = m.Id,
                Url = m.Url,
                Type = m.Type,
                UploadedAt = m.UploadedAt,
                UploadedByName = m.UploadedBy.FullName
            })
            .ToListAsync();

        return Ok(media);
    }

    [HttpPost("album/{albumId}/letter")]
    public async Task<IActionResult> AddAlbumLetter(int albumId, [FromBody] CreateLetterMediaDto dto)
    {
        var userId = GetUserId();
        var album = await _context.Albums
            .Include(a => a.Members)
            .FirstOrDefaultAsync(a => a.Id == albumId);

        if (album == null)
            return NotFound("Album was not found.");

        var canUpload = album.OwnerId == userId ||
                        album.Members.Any(m => m.UserId == userId && m.CanUpload);

        if (!canUpload)
            return Forbid();

        if (string.IsNullOrWhiteSpace(dto.Text))
            return BadRequest("Text is required.");

        var media = new Media
        {
            AlbumId = albumId,
            UploadedById = userId,
            Type = MediaType.Letter,
            Url = dto.Text.Trim(),
            UnlockAt = dto.UnlockAt
        };

        _context.MediaItems.Add(media);
        await _context.SaveChangesAsync();

        var userName = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync() ?? string.Empty;

        return Ok(new MediaResponseDto
        {
            Id = media.Id,
            Url = media.Url,
            LetterText = media.Url,
            Type = media.Type,
            UploadedAt = media.UploadedAt,
            UploadedByName = userName,
            UnlockAt = media.UnlockAt
        });
    }

    [HttpPost("album/{albumId}/photo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> AddAlbumPhoto(int albumId, IFormFile file)
    {
        var userId = GetUserId();
        var album = await _context.Albums
            .Include(a => a.Members)
            .FirstOrDefaultAsync(a => a.Id == albumId);

        if (album == null)
            return NotFound("Album was not found.");

        var canUpload = album.OwnerId == userId ||
                        album.Members.Any(m => m.UserId == userId && m.CanUpload);

        if (!canUpload)
            return Forbid();

        return await SaveImageMedia(file, new Media
        {
            AlbumId = albumId,
            UploadedById = userId,
            Type = MediaType.Image,
            Url = string.Empty
        });
    }

    [HttpPost("timecapsule/{timeCapsuleId}/photo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> AddTimeCapsulePhoto(int timeCapsuleId, IFormFile file)
    {
        var userId = GetUserId();
        var capsule = await _context.TimeCapsules
            .FirstOrDefaultAsync(tc => tc.Id == timeCapsuleId);

        if (capsule == null)
            return NotFound("Capsule was not found.");

        if (capsule.OwnerId != userId)
            return Forbid();

        return await SaveImageMedia(file, new Media
        {
            TimeCapsuleId = timeCapsuleId,
            UploadedById = userId,
            Type = MediaType.Image,
            Url = string.Empty
        });
    }

    [HttpGet("{id}/content")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMediaContent(int id)
    {
        var media = await _context.MediaItems.FirstOrDefaultAsync(m => m.Id == id);

        if (media == null || media.Type != MediaType.Image)
            return NotFound();

        var uploadsFolder = Path.Combine(
            Directory.GetCurrentDirectory(),
            "uploads",
            "media");

        if (!Directory.Exists(uploadsFolder))
            return NotFound();

        var filePath = Directory
            .EnumerateFiles(uploadsFolder, $"{id}.*")
            .FirstOrDefault();

        if (filePath == null)
            return NotFound();

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var contentType = extension switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/jpeg"
        };

        var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
        return File(bytes, contentType);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMedia(int id)
    {
        var userId = GetUserId();
        var media = await _context.MediaItems
            .Include(m => m.Album)
            .Include(m => m.TimeCapsule)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (media == null)
            return NotFound("Memory was not found.");

        var canDelete = media.UploadedById == userId ||
                        media.Album?.OwnerId == userId ||
                        media.TimeCapsule?.OwnerId == userId;

        if (!canDelete)
            return Forbid();

        _context.MediaItems.Remove(media);
        await _context.SaveChangesAsync();

        return Ok("Memory deleted.");
    }

    [HttpPut("{id}/letter")]
    public async Task<IActionResult> UpdateLetter(int id, [FromBody] CreateLetterMediaDto dto)
    {
        var userId = GetUserId();
        var media = await _context.MediaItems.FirstOrDefaultAsync(m => m.Id == id);

        if (media == null)
            return NotFound("Писмото не е намерено.");

        if (media.Type != MediaType.Letter)
            return BadRequest("Това не е писмо.");

        // Само авторът може да редактира
        if (media.UploadedById != userId)
            return Forbid();

        if (string.IsNullOrWhiteSpace(dto.Text))
            return BadRequest("Текстът е задължителен.");

        media.Url = dto.Text.Trim();
        await _context.SaveChangesAsync();

        return Ok(new MediaResponseDto
        {
            Id = media.Id,
            Url = media.Url,
            LetterText = media.Url,
            Type = media.Type,
            UploadedAt = media.UploadedAt,
            UploadedByName = userId
        });
    }
    private async Task<IActionResult> SaveImageMedia(IFormFile file, Media media)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Photo file is required.");

        if (file.ContentType == null || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only image files are allowed.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = file.ContentType.ToLowerInvariant() switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                _ => ".jpg"
            };
        }

        var allowedExtensions = new HashSet<string> { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        if (!allowedExtensions.Contains(extension))
            return BadRequest("Allowed image types: jpg, jpeg, png, webp, gif.");

        _context.MediaItems.Add(media);
        await _context.SaveChangesAsync();

        var uploadsFolder = Path.Combine(
            Directory.GetCurrentDirectory(),
            "uploads",
            "media");

        Directory.CreateDirectory(uploadsFolder);

        var storedFileName = $"{media.Id}{extension}";
        var storedPath = Path.Combine(uploadsFolder, storedFileName);

        await using (var stream = System.IO.File.Create(storedPath))
        {
            await file.CopyToAsync(stream);
        }

        media.Url = $"{Request.Scheme}://{Request.Host}/api/Media/{media.Id}/content";
        await _context.SaveChangesAsync();

        var userName = await _context.Users
            .Where(u => u.Id == media.UploadedById)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync() ?? string.Empty;

        return Ok(new MediaResponseDto
        {
            Id = media.Id,
            Url = media.Url,
            Type = media.Type,
            UploadedAt = media.UploadedAt,
            UploadedByName = userName
        });
    }
}
