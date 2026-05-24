using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Momentix.Data.Data;
using Momentix.Data.DTOs;
using Momentix.Data.Models;
using Momentix.API.Services;
using System.Security.Claims;

namespace Momentix.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TimeCapsuleController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly NotificationService _notificationService;

        public TimeCapsuleController(AppDbContext context, UserManager<User> userManager, NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Ð’Ð·ÐµÐ¼Ð¸ Ð²ÑÐ¸Ñ‡ÐºÐ¸ Ð²Ñ€ÐµÐ¼ÐµÐ²Ð¸ ÐºÐ°Ð¿ÑÑƒÐ»Ð¸ Ð½Ð° Ð¿Ð¾Ñ‚Ñ€ÐµÐ±Ð¸Ñ‚ÐµÐ»Ñ
        [HttpGet]
        public async Task<IActionResult> GetMyCapsules()
        {
            await UnlockDueCapsules();

            var userId = GetUserId();
            var now = DateTime.UtcNow;

            var capsules = await _context.TimeCapsules
                .Where(tc => tc.OwnerId == userId ||
                       tc.Members.Any(m => m.UserId == userId))
                .Include(tc => tc.Owner)
                .Include(tc => tc.Members)
                .Include(tc => tc.MediaItems)
                .Select(tc => new TimeCapsuleResponseDto
                {
                    Id = tc.Id,
                    Title = tc.Title,
                    Description = tc.Description,
                    UnlockAt = tc.UnlockAt,
                    CreatedAt = tc.CreatedAt,
                    IsUnlocked = tc.IsUnlocked || tc.UnlockAt <= now,
                    OwnerName = tc.Owner.FullName,
                    IsOwner = tc.OwnerId == userId,
                    MemberCount = tc.Members.Count,
                    MediaCount = tc.MediaItems.Count,
                    TimeRemaining = (tc.IsUnlocked || tc.UnlockAt <= now) ? null : tc.UnlockAt - now
                })
                .ToListAsync();

            return Ok(capsules);
        }

        // Ð’Ð·ÐµÐ¼Ð¸ ÐºÐ¾Ð½ÐºÑ€ÐµÑ‚Ð½Ð° ÐºÐ°Ð¿ÑÑƒÐ»Ð°
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCapsule(int id)
        {
            await UnlockDueCapsules();

            var userId = GetUserId();
            var now = DateTime.UtcNow;

            var capsule = await _context.TimeCapsules
                .Include(tc => tc.Owner)
                .Include(tc => tc.Members)
                .Include(tc => tc.MediaItems)
                .FirstOrDefaultAsync(tc => tc.Id == id);

            if (capsule == null)
                return NotFound("ÐšÐ°Ð¿ÑÑƒÐ»Ð°Ñ‚Ð° Ð½Ðµ Ðµ Ð½Ð°Ð¼ÐµÑ€ÐµÐ½Ð°.");

            bool hasAccess = capsule.OwnerId == userId ||
                             capsule.Members.Any(m => m.UserId == userId);

            if (!hasAccess)
                return Forbid();

            return Ok(new TimeCapsuleResponseDto
            {
                Id = capsule.Id,
                Title = capsule.Title,
                Description = capsule.Description,
                UnlockAt = capsule.UnlockAt,
                CreatedAt = capsule.CreatedAt,
                IsUnlocked = capsule.IsUnlocked || capsule.UnlockAt <= now,
                OwnerName = capsule.Owner.FullName,
                IsOwner = capsule.OwnerId == userId,
                MemberCount = capsule.Members.Count,
                MediaCount = capsule.MediaItems.Count,
                TimeRemaining = (capsule.IsUnlocked || capsule.UnlockAt <= now) ? null : capsule.UnlockAt - now
            });
        }

        // Ð¡ÑŠÐ·Ð´Ð°Ð¹ Ð²Ñ€ÐµÐ¼ÐµÐ²Ð° ÐºÐ°Ð¿ÑÑƒÐ»Ð°
        [HttpPost]
        public async Task<IActionResult> CreateCapsule([FromBody] CreateTimeCapsuleDto dto)
        {
            var userId = GetUserId();

            if (dto.UnlockAt <= DateTime.UtcNow)
                return BadRequest("Ð”Ð°Ñ‚Ð°Ñ‚Ð° Ð½Ð° Ð¾Ñ‚ÐºÐ»ÑŽÑ‡Ð²Ð°Ð½Ðµ Ñ‚Ñ€ÑÐ±Ð²Ð° Ð´Ð° Ðµ Ð² Ð±ÑŠÐ´ÐµÑ‰ÐµÑ‚Ð¾.");

            var capsule = new TimeCapsule
            {
                Title = dto.Title,
                Description = dto.Description,
                UnlockAt = dto.UnlockAt,
                OwnerId = userId
            };

            _context.TimeCapsules.Add(capsule);
            await _context.SaveChangesAsync();

            var owner = await _userManager.FindByIdAsync(userId);

            return Ok(new TimeCapsuleResponseDto
            {
                Id = capsule.Id,
                Title = capsule.Title,
                Description = capsule.Description,
                UnlockAt = capsule.UnlockAt,
                CreatedAt = capsule.CreatedAt,
                IsUnlocked = false,
                OwnerName = owner!.FullName,
                IsOwner = true,
                MemberCount = 0,
                MediaCount = 0,
                TimeRemaining = capsule.UnlockAt - DateTime.UtcNow
            });
        }

        // ÐŸÑ€Ð¾Ð¼ÐµÐ½Ð¸ Ð´Ð°Ñ‚Ð°Ñ‚Ð° Ð½Ð° Ð¾Ñ‚ÐºÐ»ÑŽÑ‡Ð²Ð°Ð½Ðµ (ÑÐ°Ð¼Ð¾ ÑÐ¾Ð±ÑÑ‚Ð²ÐµÐ½Ð¸ÐºÑŠÑ‚)
        [HttpPut("{id}/unlock-date")]
        public async Task<IActionResult> UpdateUnlockDate(int id, [FromBody] UpdateUnlockDateDto dto)
        {
            var userId = GetUserId();

            var capsule = await _context.TimeCapsules.FirstOrDefaultAsync(tc => tc.Id == id);

            if (capsule == null)
                return NotFound("ÐšÐ°Ð¿ÑÑƒÐ»Ð°Ñ‚Ð° Ð½Ðµ Ðµ Ð½Ð°Ð¼ÐµÑ€ÐµÐ½Ð°.");

            if (capsule.OwnerId != userId)
                return Forbid();

            if (capsule.IsUnlocked)
                return BadRequest("ÐšÐ°Ð¿ÑÑƒÐ»Ð°Ñ‚Ð° Ð²ÐµÑ‡Ðµ Ðµ Ð¾Ñ‚ÐºÐ»ÑŽÑ‡ÐµÐ½Ð°.");

            if (dto.NewUnlockAt <= DateTime.UtcNow)
                return BadRequest("Ð”Ð°Ñ‚Ð°Ñ‚Ð° Ð½Ð° Ð¾Ñ‚ÐºÐ»ÑŽÑ‡Ð²Ð°Ð½Ðµ Ñ‚Ñ€ÑÐ±Ð²Ð° Ð´Ð° Ðµ Ð² Ð±ÑŠÐ´ÐµÑ‰ÐµÑ‚Ð¾.");

            capsule.UnlockAt = dto.NewUnlockAt;
            await _context.SaveChangesAsync();

            return Ok("Ð”Ð°Ñ‚Ð°Ñ‚Ð° Ð½Ð° Ð¾Ñ‚ÐºÐ»ÑŽÑ‡Ð²Ð°Ð½Ðµ Ðµ Ð¾Ð±Ð½Ð¾Ð²ÐµÐ½Ð° ÑƒÑÐ¿ÐµÑˆÐ½Ð¾.");
        }

        [HttpGet("{id}/members")]
        public async Task<IActionResult> GetMembers(int id)
        {
            var userId = GetUserId();

            var capsule = await _context.TimeCapsules
                .Include(tc => tc.Owner)
                .Include(tc => tc.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(tc => tc.Id == id);

            if (capsule == null)
                return NotFound("Capsule was not found.");

            var hasAccess = capsule.OwnerId == userId ||
                            capsule.Members.Any(m => m.UserId == userId);

            if (!hasAccess)
                return Forbid();

            var members = capsule.Members.Select(m => new AlbumMemberResponseDto
            {
                UserId = m.UserId,
                FullName = m.User.FullName,
                CanUpload = false,
                IsOwner = false
            }).ToList();

            members.Insert(0, new AlbumMemberResponseDto
            {
                UserId = capsule.OwnerId,
                FullName = capsule.Owner.FullName,
                CanUpload = true,
                IsOwner = true
            });

            return Ok(members);
        }
        // Ð”Ð¾Ð±Ð°Ð²Ð¸ Ñ‡Ð»ÐµÐ½ ÐºÑŠÐ¼ ÐºÐ°Ð¿ÑÑƒÐ»Ð° (ÑÐ°Ð¼Ð¾ ÑÐ¾Ð±ÑÑ‚Ð²ÐµÐ½Ð¸ÐºÑŠÑ‚)
        [HttpPost("{id}/members")]
        public async Task<IActionResult> AddMember(int id, [FromBody] AddAlbumMemberDto dto)
        {
            var userId = GetUserId();

            var capsule = await _context.TimeCapsules
                .Include(tc => tc.Members)
                .FirstOrDefaultAsync(tc => tc.Id == id);

            if (capsule == null)
                return NotFound("ÐšÐ°Ð¿ÑÑƒÐ»Ð°Ñ‚Ð° Ð½Ðµ Ðµ Ð½Ð°Ð¼ÐµÑ€ÐµÐ½Ð°.");

            if (capsule.OwnerId != userId)
                return Forbid();

            var userToAdd = await _userManager.FindByEmailAsync(dto.UserEmail);
            if (userToAdd == null)
                return NotFound("ÐŸÐ¾Ñ‚Ñ€ÐµÐ±Ð¸Ñ‚ÐµÐ»ÑÑ‚ Ð½Ðµ Ðµ Ð½Ð°Ð¼ÐµÑ€ÐµÐ½.");

            if (userToAdd.Id == capsule.OwnerId)
                return BadRequest("Owner already has access to the capsule.");
            bool alreadyMember = capsule.Members.Any(m => m.UserId == userToAdd.Id);
            if (alreadyMember)
                return BadRequest("ÐŸÐ¾Ñ‚Ñ€ÐµÐ±Ð¸Ñ‚ÐµÐ»ÑÑ‚ Ð²ÐµÑ‡Ðµ Ðµ Ñ‡Ð»ÐµÐ½ Ð½Ð° ÐºÐ°Ð¿ÑÑƒÐ»Ð°Ñ‚Ð°.");

            var member = new TimeCapsuleMember
            {
                TimeCapsuleId = id,
                UserId = userToAdd.Id
            };

            _context.TimeCapsuleMembers.Add(member);

            var ownerName = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync() ?? "Someone";

            _notificationService.Add(
                userToAdd.Id,
                "Time capsule shared",
                $"{ownerName} shared time capsule \"{capsule.Title}\" with you.",
                NotificationType.CapsuleShared,
                "TimeCapsule",
                capsule.Id,
                userId);

            await _context.SaveChangesAsync();

            return Ok("Ð§Ð»ÐµÐ½ÑŠÑ‚ Ðµ Ð´Ð¾Ð±Ð°Ð²ÐµÐ½ ÑƒÑÐ¿ÐµÑˆÐ½Ð¾.");
        }

        // Ð ÑŠÑ‡Ð½Ð¾ Ð¾Ñ‚ÐºÐ»ÑŽÑ‡Ð²Ð°Ð½Ðµ (ÑÐ°Ð¼Ð¾ Ð·Ð° Ñ‚ÐµÑÑ‚Ð¾Ð²Ðµ)
        [HttpPost("{id}/unlock")]
        public async Task<IActionResult> UnlockCapsule(int id)
        {
            var userId = GetUserId();

            var capsule = await _context.TimeCapsules.FirstOrDefaultAsync(tc => tc.Id == id);

            if (capsule == null)
                return NotFound("ÐšÐ°Ð¿ÑÑƒÐ»Ð°Ñ‚Ð° Ð½Ðµ Ðµ Ð½Ð°Ð¼ÐµÑ€ÐµÐ½Ð°.");

            if (capsule.OwnerId != userId)
                return Forbid();

            if (capsule.IsUnlocked)
                return BadRequest("ÐšÐ°Ð¿ÑÑƒÐ»Ð°Ñ‚Ð° Ð²ÐµÑ‡Ðµ Ðµ Ð¾Ñ‚ÐºÐ»ÑŽÑ‡ÐµÐ½Ð°.");

            if (capsule.UnlockAt > DateTime.UtcNow)
                return BadRequest($"ÐšÐ°Ð¿ÑÑƒÐ»Ð°Ñ‚Ð° Ñ‰Ðµ ÑÐµ Ð¾Ñ‚ÐºÐ»ÑŽÑ‡Ð¸ Ð½Ð° {capsule.UnlockAt:dd.MM.yyyy HH:mm}.");

            capsule.IsUnlocked = true;
            await _context.SaveChangesAsync();

            return Ok("ÐšÐ°Ð¿ÑÑƒÐ»Ð°Ñ‚Ð° Ðµ Ð¾Ñ‚ÐºÐ»ÑŽÑ‡ÐµÐ½Ð° ÑƒÑÐ¿ÐµÑˆÐ½Ð¾!");
        }

        // Ð˜Ð·Ñ‚Ñ€Ð¸Ð¹ ÐºÐ°Ð¿ÑÑƒÐ»Ð° (ÑÐ°Ð¼Ð¾ ÑÐ¾Ð±ÑÑ‚Ð²ÐµÐ½Ð¸ÐºÑŠÑ‚)
        private async Task UnlockDueCapsules()
        {
            var now = DateTime.UtcNow;
            var capsules = await _context.TimeCapsules
                .Include(tc => tc.Members)
                .Where(tc => !tc.IsUnlocked && tc.UnlockAt <= now)
                .ToListAsync();

            foreach (var capsule in capsules)
            {
                capsule.IsUnlocked = true;
                var recipients = capsule.Members
                    .Select(m => m.UserId)
                    .Append(capsule.OwnerId);

                _notificationService.AddForUsers(
                    recipients,
                    "Time capsule unlocked",
                    $"Time capsule \"{capsule.Title}\" is now open.",
                    NotificationType.CapsuleUnlocked,
                    "TimeCapsule",
                    capsule.Id,
                    capsule.OwnerId);
            }

            if (capsules.Count > 0)
                await _context.SaveChangesAsync();
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCapsule(int id)
        {
            var userId = GetUserId();

            var capsule = await _context.TimeCapsules.FirstOrDefaultAsync(tc => tc.Id == id);

            if (capsule == null)
                return NotFound("ÐšÐ°Ð¿ÑÑƒÐ»Ð°Ñ‚Ð° Ð½Ðµ Ðµ Ð½Ð°Ð¼ÐµÑ€ÐµÐ½Ð°.");

            if (capsule.OwnerId != userId)
                return Forbid();

            _context.TimeCapsules.Remove(capsule);
            await _context.SaveChangesAsync();

            return Ok("ÐšÐ°Ð¿ÑÑƒÐ»Ð°Ñ‚Ð° Ðµ Ð¸Ð·Ñ‚Ñ€Ð¸Ñ‚Ð° ÑƒÑÐ¿ÐµÑˆÐ½Ð¾.");
        }
    }
}




