using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Momentix.API.Data;
using Momentix.API.DTOs;
using Momentix.API.Models;
using System.Security.Claims;

namespace Momentix.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AlbumsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public AlbumsController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Вземи всички албуми на потребителя
        [HttpGet]
        public async Task<IActionResult> GetMyAlbums()
        {
            var userId = GetUserId();

            var albums = await _context.Albums
                .Where(a => a.OwnerId == userId ||
                       a.Members.Any(m => m.UserId == userId))
                .Include(a => a.Owner)
                .Include(a => a.Members)
                .Include(a => a.MediaItems)
                .Select(a => new AlbumResponseDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Description = a.Description,
                    CreatedAt = a.CreatedAt,
                    OwnerName = a.Owner.FullName,
                    MemberCount = a.Members.Count,
                    MediaCount = a.MediaItems.Count
                })
                .ToListAsync();

            return Ok(albums);
        }

        // Вземи конкретен албум
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAlbum(int id)
        {
            var userId = GetUserId();

            var album = await _context.Albums
                .Include(a => a.Owner)
                .Include(a => a.Members)
                .Include(a => a.MediaItems)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (album == null)
                return NotFound("Албумът не е намерен.");

            bool hasAccess = album.OwnerId == userId ||
                             album.Members.Any(m => m.UserId == userId);

            if (!hasAccess)
                return Forbid();

            return Ok(new AlbumResponseDto
            {
                Id = album.Id,
                Title = album.Title,
                Description = album.Description,
                CreatedAt = album.CreatedAt,
                OwnerName = album.Owner.FullName,
                MemberCount = album.Members.Count,
                MediaCount = album.MediaItems.Count
            });
        }

        // Създай албум
        [HttpPost]
        public async Task<IActionResult> CreateAlbum([FromBody] CreateAlbumDto dto)
        {
            var userId = GetUserId();

            var album = new Album
            {
                Title = dto.Title,
                Description = dto.Description,
                OwnerId = userId
            };

            _context.Albums.Add(album);
            await _context.SaveChangesAsync();

            return Ok(new AlbumResponseDto
            {
                Id = album.Id,
                Title = album.Title,
                Description = album.Description,
                CreatedAt = album.CreatedAt,
                OwnerName = (await _userManager.FindByIdAsync(userId))!.FullName,
                MemberCount = 0,
                MediaCount = 0
            });
        }

        // Изтрий албум (само собственикът)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAlbum(int id)
        {
            var userId = GetUserId();

            var album = await _context.Albums.FirstOrDefaultAsync(a => a.Id == id);

            if (album == null)
                return NotFound("Албумът не е намерен.");

            if (album.OwnerId != userId)
                return Forbid();

            _context.Albums.Remove(album);
            await _context.SaveChangesAsync();

            return Ok("Албумът е изтрит успешно.");
        }

        // Добави член към албум (само собственикът)
        [HttpPost("{id}/members")]
        public async Task<IActionResult> AddMember(int id, [FromBody] AddAlbumMemberDto dto)
        {
            var userId = GetUserId();

            var album = await _context.Albums
                .Include(a => a.Members)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (album == null)
                return NotFound("Албумът не е намерен.");

            if (album.OwnerId != userId)
                return Forbid();

            var userToAdd = await _userManager.FindByEmailAsync(dto.UserEmail);
            if (userToAdd == null)
                return NotFound("Потребителят не е намерен.");

            bool alreadyMember = album.Members.Any(m => m.UserId == userToAdd.Id);
            if (alreadyMember)
                return BadRequest("Потребителят вече е член на албума.");

            var member = new AlbumMember
            {
                AlbumId = id,
                UserId = userToAdd.Id,
                CanUpload = dto.CanUpload
            };

            _context.AlbumMembers.Add(member);
            await _context.SaveChangesAsync();

            return Ok("Членът е добавен успешно.");
        }

        // Премахни член от албум (само собственикът)
        [HttpDelete("{id}/members/{memberId}")]
        public async Task<IActionResult> RemoveMember(int id, string memberId)
        {
            var userId = GetUserId();

            var album = await _context.Albums.FirstOrDefaultAsync(a => a.Id == id);

            if (album == null)
                return NotFound("Албумът не е намерен.");

            if (album.OwnerId != userId)
                return Forbid();

            var member = await _context.AlbumMembers
                .FirstOrDefaultAsync(m => m.AlbumId == id && m.UserId == memberId);

            if (member == null)
                return NotFound("Членът не е намерен.");

            _context.AlbumMembers.Remove(member);
            await _context.SaveChangesAsync();

            return Ok("Членът е премахнат успешно.");
        }
    }
}