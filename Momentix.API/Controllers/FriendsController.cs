using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Momentix.Data.Data;
using Momentix.Data.DTOs;
using Momentix.Data.Models;
using Momentix.API.Services;
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
    private readonly NotificationService _notificationService;

    public FriendsController(AppDbContext context, UserManager<User> userManager, NotificationService notificationService)
    {
        _context = context;
        _userManager = userManager;
        _notificationService = notificationService;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private async Task EnsureFriendsTables()
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
                CREATE TABLE IF NOT EXISTS `FriendRequests` (
                    `Id` int NOT NULL AUTO_INCREMENT,
                    `RequesterId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
                    `AddresseeId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
                    `Status` int NOT NULL,
                    `CreatedAt` datetime(6) NOT NULL,
                    `RespondedAt` datetime(6) NULL,
                    CONSTRAINT `PK_FriendRequests` PRIMARY KEY (`Id`),
                    CONSTRAINT `FK_FriendRequests_AspNetUsers_AddresseeId` FOREIGN KEY (`AddresseeId`) REFERENCES `AspNetUsers` (`Id`),
                    CONSTRAINT `FK_FriendRequests_AspNetUsers_RequesterId` FOREIGN KEY (`RequesterId`) REFERENCES `AspNetUsers` (`Id`),
                    INDEX `IX_FriendRequests_AddresseeId` (`AddresseeId`),
                    UNIQUE INDEX `IX_FriendRequests_RequesterId_AddresseeId` (`RequesterId`, `AddresseeId`)
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

            await _context.Database.ExecuteSqlRawAsync("""
                INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
                VALUES ('20260512212633_AddFriendRequests', '8.0.0');
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
        await EnsureFriendsTables();

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

    [HttpGet("users")]
    public async Task<IActionResult> SearchUsers([FromQuery] string? search)
    {
        await EnsureFriendsTables();

        var userId = GetUserId();
        var query = _context.Users.Where(u => u.Id != userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                u.FullName.Contains(term) ||
                (u.Email != null && u.Email.Contains(term)));
        }

        var users = await query
            .OrderBy(u => u.FullName)
            .Take(50)
            .ToListAsync();

        var friendIds = await _context.Friends
            .Where(f => f.UserId == userId)
            .Select(f => f.FriendUserId)
            .ToListAsync();

        var pendingRequests = await _context.FriendRequests
            .Where(fr => fr.Status == FriendRequestStatus.Pending &&
                         (fr.RequesterId == userId || fr.AddresseeId == userId))
            .ToListAsync();

        var response = users.Select(u =>
        {
            var incoming = pendingRequests.FirstOrDefault(fr => fr.RequesterId == u.Id && fr.AddresseeId == userId);

            return new UserSearchResponseDto
            {
                UserId = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                IsFriend = friendIds.Contains(u.Id),
                HasPendingOutgoingRequest = pendingRequests.Any(fr => fr.RequesterId == userId && fr.AddresseeId == u.Id),
                HasPendingIncomingRequest = incoming != null,
                IncomingRequestId = incoming?.Id
            };
        }).ToList();

        return Ok(response);
    }

    [HttpGet("requests/incoming")]
    public async Task<IActionResult> GetIncomingRequests()
    {
        await EnsureFriendsTables();

        var userId = GetUserId();

        var requests = await _context.FriendRequests
            .Where(fr => fr.AddresseeId == userId && fr.Status == FriendRequestStatus.Pending)
            .Include(fr => fr.Requester)
            .Include(fr => fr.Addressee)
            .OrderByDescending(fr => fr.CreatedAt)
            .ToListAsync();

        return Ok(requests.Select(ToRequestDto).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> SendFriendRequestByEmail([FromBody] AddFriendDto dto)
    {
        await EnsureFriendsTables();

        var email = dto.Email.Trim();
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("Email is required.");

        var targetUser = await _userManager.FindByEmailAsync(email);
        if (targetUser == null)
            return NotFound("User was not found.");

        return await SendFriendRequestToUser(targetUser.Id);
    }

    [HttpPost("requests/{targetUserId}")]
    public async Task<IActionResult> SendFriendRequest(string targetUserId)
    {
        await EnsureFriendsTables();
        return await SendFriendRequestToUser(targetUserId);
    }

    [HttpPost("requests/{requestId:int}/accept")]
    public async Task<IActionResult> AcceptRequest(int requestId)
    {
        await EnsureFriendsTables();

        var userId = GetUserId();
        var request = await _context.FriendRequests
            .Include(fr => fr.Requester)
            .Include(fr => fr.Addressee)
            .FirstOrDefaultAsync(fr => fr.Id == requestId);

        if (request == null)
            return NotFound("Friend request was not found.");

        if (request.AddresseeId != userId)
            return Forbid();

        if (request.Status != FriendRequestStatus.Pending)
            return BadRequest("Friend request is not pending.");

        request.Status = FriendRequestStatus.Accepted;
        request.RespondedAt = DateTime.UtcNow;

        await AddMutualFriendship(request.RequesterId, request.AddresseeId);
        // AcceptRequest:
        _notificationService.Add(
            request.RequesterId,
            "Заявката за приятелство е приета",
            $"{request.Addressee.FullName} прие заявката ти за приятелство.",
            NotificationType.FriendAccepted,
            "Friend",
            null,
            request.AddresseeId);
        await _context.SaveChangesAsync();

        return Ok(ToRequestDto(request));
    }

    [HttpPost("requests/{requestId:int}/decline")]
    public async Task<IActionResult> DeclineRequest(int requestId)
    {
        await EnsureFriendsTables();

        var userId = GetUserId();
        var request = await _context.FriendRequests
            .Include(fr => fr.Requester)
            .Include(fr => fr.Addressee)
            .FirstOrDefaultAsync(fr => fr.Id == requestId);

        if (request == null)
            return NotFound("Friend request was not found.");

        if (request.AddresseeId != userId)
            return Forbid();

        if (request.Status != FriendRequestStatus.Pending)
            return BadRequest("Friend request is not pending.");

        request.Status = FriendRequestStatus.Declined;
        request.RespondedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(ToRequestDto(request));
    }

    [HttpDelete("{friendUserId}")]
    public async Task<IActionResult> RemoveFriend(string friendUserId)
    {
        await EnsureFriendsTables();

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

    private async Task<IActionResult> SendFriendRequestToUser(string targetUserId)
    {
        var userId = GetUserId();

        if (targetUserId == userId)
            return BadRequest("You cannot send a friend request to yourself.");

        var targetExists = await _context.Users.AnyAsync(u => u.Id == targetUserId);
        if (!targetExists)
            return NotFound("User was not found.");

        var alreadyFriends = await _context.Friends
            .AnyAsync(f => f.UserId == userId && f.FriendUserId == targetUserId);

        if (alreadyFriends)
            return BadRequest("This user is already in your friend list.");

        var incomingRequest = await _context.FriendRequests
            .Include(fr => fr.Requester)
            .Include(fr => fr.Addressee)
            .FirstOrDefaultAsync(fr =>
                fr.RequesterId == targetUserId &&
                fr.AddresseeId == userId &&
                fr.Status == FriendRequestStatus.Pending);

        if (incomingRequest != null)
            return await AcceptRequest(incomingRequest.Id);

        var existingRequest = await _context.FriendRequests
            .Include(fr => fr.Requester)
            .Include(fr => fr.Addressee)
            .FirstOrDefaultAsync(fr => fr.RequesterId == userId && fr.AddresseeId == targetUserId);

        if (existingRequest != null)
        {
            if (existingRequest.Status == FriendRequestStatus.Pending)
                return BadRequest("Friend request is already pending.");

            existingRequest.Status = FriendRequestStatus.Pending;
            existingRequest.CreatedAt = DateTime.UtcNow;
            existingRequest.RespondedAt = null;
            // SendFriendRequestToUser (съществуваща):
            _notificationService.Add(
                targetUserId,
                "Заявка за приятелство",
                $"{existingRequest.Requester.FullName} ти изпрати заявка за приятелство.",
                NotificationType.FriendRequest,
                "FriendRequest",
                existingRequest.Id,
                userId);
            await _context.SaveChangesAsync();

            return Ok(ToRequestDto(existingRequest));
        }

        var request = new FriendRequest
        {
            RequesterId = userId,
            AddresseeId = targetUserId,
            Status = FriendRequestStatus.Pending
        };

        _context.FriendRequests.Add(request);
        await _context.SaveChangesAsync();

        request = await _context.FriendRequests
            .Include(fr => fr.Requester)
            .Include(fr => fr.Addressee)
            .FirstAsync(fr => fr.Id == request.Id);

        // SendFriendRequestToUser (нова):
        _notificationService.Add(
            targetUserId,
            "Заявка за приятелство",
            $"{request.Requester.FullName} ти изпрати заявка за приятелство.",
            NotificationType.FriendRequest,
            "FriendRequest",
            request.Id,
            userId);
        await _context.SaveChangesAsync();

        return Ok(ToRequestDto(request));
    }

    private async Task AddMutualFriendship(string firstUserId, string secondUserId)
    {
        var now = DateTime.UtcNow;

        var firstExists = await _context.Friends
            .AnyAsync(f => f.UserId == firstUserId && f.FriendUserId == secondUserId);
        if (!firstExists)
        {
            _context.Friends.Add(new Friend
            {
                UserId = firstUserId,
                FriendUserId = secondUserId,
                CreatedAt = now
            });
        }

        var secondExists = await _context.Friends
            .AnyAsync(f => f.UserId == secondUserId && f.FriendUserId == firstUserId);
        if (!secondExists)
        {
            _context.Friends.Add(new Friend
            {
                UserId = secondUserId,
                FriendUserId = firstUserId,
                CreatedAt = now
            });
        }
    }

    private static FriendRequestResponseDto ToRequestDto(FriendRequest request) =>
        new()
        {
            Id = request.Id,
            RequesterId = request.RequesterId,
            RequesterName = request.Requester.FullName,
            RequesterEmail = request.Requester.Email ?? string.Empty,
            AddresseeId = request.AddresseeId,
            AddresseeName = request.Addressee.FullName,
            AddresseeEmail = request.Addressee.Email ?? string.Empty,
            Status = request.Status,
            CreatedAt = request.CreatedAt
        };
}


