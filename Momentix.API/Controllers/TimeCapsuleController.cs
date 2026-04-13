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
    public class TimeCapsuleController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public TimeCapsuleController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Вземи всички времеви капсули на потребителя
        [HttpGet]
        public async Task<IActionResult> GetMyCapsules()
        {
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
                    IsUnlocked = tc.IsUnlocked,
                    OwnerName = tc.Owner.FullName,
                    MemberCount = tc.Members.Count,
                    MediaCount = tc.MediaItems.Count,
                    TimeRemaining = tc.IsUnlocked ? null : (tc.UnlockAt > now ? tc.UnlockAt - now : TimeSpan.Zero)
                })
                .ToListAsync();

            return Ok(capsules);
        }

        // Вземи конкретна капсула
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCapsule(int id)
        {
            var userId = GetUserId();
            var now = DateTime.UtcNow;

            var capsule = await _context.TimeCapsules
                .Include(tc => tc.Owner)
                .Include(tc => tc.Members)
                .Include(tc => tc.MediaItems)
                .FirstOrDefaultAsync(tc => tc.Id == id);

            if (capsule == null)
                return NotFound("Капсулата не е намерена.");

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
                IsUnlocked = capsule.IsUnlocked,
                OwnerName = capsule.Owner.FullName,
                MemberCount = capsule.Members.Count,
                MediaCount = capsule.MediaItems.Count,
                TimeRemaining = capsule.IsUnlocked ? null : (capsule.UnlockAt > now ? capsule.UnlockAt - now : TimeSpan.Zero)
            });
        }

        // Създай времева капсула
        [HttpPost]
        public async Task<IActionResult> CreateCapsule([FromBody] CreateTimeCapsuleDto dto)
        {
            var userId = GetUserId();

            if (dto.UnlockAt <= DateTime.UtcNow)
                return BadRequest("Датата на отключване трябва да е в бъдещето.");

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
                MemberCount = 0,
                MediaCount = 0,
                TimeRemaining = capsule.UnlockAt - DateTime.UtcNow
            });
        }

        // Промени датата на отключване (само собственикът)
        [HttpPut("{id}/unlock-date")]
        public async Task<IActionResult> UpdateUnlockDate(int id, [FromBody] UpdateUnlockDateDto dto)
        {
            var userId = GetUserId();

            var capsule = await _context.TimeCapsules.FirstOrDefaultAsync(tc => tc.Id == id);

            if (capsule == null)
                return NotFound("Капсулата не е намерена.");

            if (capsule.OwnerId != userId)
                return Forbid();

            if (capsule.IsUnlocked)
                return BadRequest("Капсулата вече е отключена.");

            if (dto.NewUnlockAt <= DateTime.UtcNow)
                return BadRequest("Датата на отключване трябва да е в бъдещето.");

            capsule.UnlockAt = dto.NewUnlockAt;
            await _context.SaveChangesAsync();

            return Ok("Датата на отключване е обновена успешно.");
        }

        // Добави член към капсула (само собственикът)
        [HttpPost("{id}/members")]
        public async Task<IActionResult> AddMember(int id, [FromBody] AddAlbumMemberDto dto)
        {
            var userId = GetUserId();

            var capsule = await _context.TimeCapsules
                .Include(tc => tc.Members)
                .FirstOrDefaultAsync(tc => tc.Id == id);

            if (capsule == null)
                return NotFound("Капсулата не е намерена.");

            if (capsule.OwnerId != userId)
                return Forbid();

            var userToAdd = await _userManager.FindByEmailAsync(dto.UserEmail);
            if (userToAdd == null)
                return NotFound("Потребителят не е намерен.");

            bool alreadyMember = capsule.Members.Any(m => m.UserId == userToAdd.Id);
            if (alreadyMember)
                return BadRequest("Потребителят вече е член на капсулата.");

            var member = new TimeCapsuleMember
            {
                TimeCapsuleId = id,
                UserId = userToAdd.Id
            };

            _context.TimeCapsuleMembers.Add(member);
            await _context.SaveChangesAsync();

            return Ok("Членът е добавен успешно.");
        }

        // Ръчно отключване (само за тестове)
        [HttpPost("{id}/unlock")]
        public async Task<IActionResult> UnlockCapsule(int id)
        {
            var userId = GetUserId();

            var capsule = await _context.TimeCapsules.FirstOrDefaultAsync(tc => tc.Id == id);

            if (capsule == null)
                return NotFound("Капсулата не е намерена.");

            if (capsule.OwnerId != userId)
                return Forbid();

            if (capsule.IsUnlocked)
                return BadRequest("Капсулата вече е отключена.");

            if (capsule.UnlockAt > DateTime.UtcNow)
                return BadRequest($"Капсулата ще се отключи на {capsule.UnlockAt:dd.MM.yyyy HH:mm}.");

            capsule.IsUnlocked = true;
            await _context.SaveChangesAsync();

            return Ok("Капсулата е отключена успешно!");
        }

        // Изтрий капсула (само собственикът)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCapsule(int id)
        {
            var userId = GetUserId();

            var capsule = await _context.TimeCapsules.FirstOrDefaultAsync(tc => tc.Id == id);

            if (capsule == null)
                return NotFound("Капсулата не е намерена.");

            if (capsule.OwnerId != userId)
                return Forbid();

            _context.TimeCapsules.Remove(capsule);
            await _context.SaveChangesAsync();

            return Ok("Капсулата е изтрита успешно.");
        }
    }
}