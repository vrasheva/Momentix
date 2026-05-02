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
            Url = dto.Text.Trim()
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
            Type = media.Type,
            UploadedAt = media.UploadedAt,
            UploadedByName = userName
        });
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
}
