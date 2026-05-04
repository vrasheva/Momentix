using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
public class FriendsController : ControllerBase
{
    private static readonly SemaphoreSlim FriendsTableLock = new(1, 1);
    private static bool _isFriendsTableReady;

    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;

    public FriendsController(AppDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private async Task EnsureFriendsTable()
    {
        if (_isFriendsTableReady)
            return;

        await FriendsTableLock.WaitAsync();

        try
        {
            if (_isFriendsTableReady)
                return;

            await _context.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS `Friends` (
                    `Id` int NOT NULL AUTO_INCREMENT,
                    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
                    `FriendUserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
                    `CreatedAt` datetime(6) NOT NULL,
                    CONSTRAINT `PK_Friends` PRIMARY KEY (`Id`),
                    CONSTRAINT `FK_Friends_AspNetUsers_FriendUserId` FOREIGN KEY (`FriendUserId`) REFERENCES `AspNetUsers` (`Id`),
                    CONSTRAINT `FK_Friends_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`),
                    INDEX `IX_Friends_FriendUserId` (`FriendUserId`),
                    UNIQUE INDEX `IX_Friends_UserId_FriendUserId` (`UserId`, `FriendUserId`)
                ) CHARACTER SET=utf8mb4;
                """);

            await _context.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
                    `MigrationId` varchar(150) NOT NULL,
                    `ProductVersion` varchar(32) NOT NULL,
                    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
                );
                """);

            await _context.Database.ExecuteSqlRawAsync("""
                INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
                VALUES ('20260427194051_InitialCreate', '8.0.0');
                """);

            await _context.Database.ExecuteSqlRawAsync("""
                INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
                VALUES ('20260504131557_AddFriends', '8.0.0');
                """);

            _isFriendsTableReady = true;
        }
        finally
        {
            FriendsTableLock.Release();
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetFriends()
    {
        await EnsureFriendsTable();

        var userId = GetUserId();

        var friends = await _context.Friends
            .Where(f => f.UserId == userId)
            .Include(f => f.FriendUser)
            .OrderBy(f => f.FriendUser.FullName)
            .Select(f => new FriendResponseDto
            {
                UserId = f.FriendUser.Id,
                FullName = f.FriendUser.FullName,
                Email = f.FriendUser.Email ?? string.Empty,
                ProfilePictureUrl = f.FriendUser.ProfilePictureUrl,
                AddedAt = f.CreatedAt
            })
            .ToListAsync();

        return Ok(friends);
    }

    [HttpPost]
    public async Task<IActionResult> AddFriend([FromBody] AddFriendDto dto)
    {
        await EnsureFriendsTable();

        var userId = GetUserId();
        var email = dto.Email.Trim();

        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("Email is required.");

        var friendUser = await _userManager.FindByEmailAsync(email);
        if (friendUser == null)
            return NotFound("User was not found.");

        if (friendUser.Id == userId)
            return BadRequest("You cannot add yourself as a friend.");

        var alreadyFriends = await _context.Friends
            .AnyAsync(f => f.UserId == userId && f.FriendUserId == friendUser.Id);

        if (alreadyFriends)
            return BadRequest("This user is already in your friend list.");

        var now = DateTime.UtcNow;
        _context.Friends.Add(new Friend
        {
            UserId = userId,
            FriendUserId = friendUser.Id,
            CreatedAt = now
        });

        _context.Friends.Add(new Friend
        {
            UserId = friendUser.Id,
            FriendUserId = userId,
            CreatedAt = now
        });

        await _context.SaveChangesAsync();

        return Ok(new FriendResponseDto
        {
            UserId = friendUser.Id,
            FullName = friendUser.FullName,
            Email = friendUser.Email ?? string.Empty,
            ProfilePictureUrl = friendUser.ProfilePictureUrl,
            AddedAt = now
        });
    }

    [HttpDelete("{friendUserId}")]
    public async Task<IActionResult> RemoveFriend(string friendUserId)
    {
        await EnsureFriendsTable();

        var userId = GetUserId();

        var friendships = await _context.Friends
            .Where(f =>
                (f.UserId == userId && f.FriendUserId == friendUserId) ||
                (f.UserId == friendUserId && f.FriendUserId == userId))
            .ToListAsync();

        if (friendships.Count == 0)
            return NotFound("Friend was not found.");

        _context.Friends.RemoveRange(friendships);
        await _context.SaveChangesAsync();

        return Ok("Friend removed.");
    }
}
