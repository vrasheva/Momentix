using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Momentix.API.Services;
using Momentix.Data.Data;
using Momentix.Data.DTOs;
using Momentix.Data.Models;
using System.Security.Claims;

namespace Momentix.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChallengesController : ControllerBase
{
    private const string AdminRole = "Admin";

    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly NotificationService _notificationService;
    private readonly ChallengeVisionService _challengeVisionService;

    private static readonly string[] DailyPrompts =
{
    "Снимай нещо зелено",
    "Снимай нещо блестящо",
    "Снимай нещо кръгло",
    "Снимай нещо синьо",
    "Снимай нещо малко",
    "Снимай нещо метално",
    "Снимай нещо с букви",
    "Снимай нещо меко",
    "Снимай нещо старо",
    "Снимай нещо с отражение",
    "Снимай нещо от природата",
    "Снимай нещо, което използваш всеки ден",
    "Снимай нещо ръчно изработено",
    "Снимай нещо, което те е накарало да се усмихнеш"
};

    public ChallengesController(
        AppDbContext context,
        UserManager<User> userManager,
        NotificationService notificationService,
        ChallengeVisionService challengeVisionService)
    {
        _context = context;
        _userManager = userManager;
        _notificationService = notificationService;
        _challengeVisionService = challengeVisionService;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveChallenge()
    {
        var challenge = await GetOrCreateDailyChallenge();
        return Ok(ToDto(challenge));
    }

    [HttpGet("{challengeId}/submissions")]
    public async Task<IActionResult> GetSubmissions(int challengeId)
    {
        var userId = GetUserId();
        var allowedUserIds = await GetFriendIds(userId);
        allowedUserIds.Add(userId);

        var submissions = await _context.ChallengeSubmissions
            .Where(s => s.ChallengeId == challengeId && allowedUserIds.Contains(s.UserId))
            .Include(s => s.User)
            .Include(s => s.Votes)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => new ChallengeSubmissionResponseDto
            {
                Id = s.Id,
                ChallengeId = s.ChallengeId,
                UserName = s.User.FullName,
                MediaUrl = s.MediaUrl,
                SubmittedAt = s.SubmittedAt,
                VoteCount = s.Votes.Count,
                AiIsSatisfied = s.AiIsSatisfied,
                AiConfidence = s.AiConfidence,
                AiFeedback = s.AiFeedback,
                AiModel = s.AiModel,
                AiEvaluatedAt = s.AiEvaluatedAt
            })
            .ToListAsync();

        return Ok(submissions);
    }

    [HttpPost("{challengeId}/submissions")]
    public async Task<IActionResult> Submit(int challengeId, [FromBody] CreateChallengeSubmissionDto dto)
    {
        var userId = GetUserId();
        var challengeExists = await _context.Challenges.AnyAsync(c => c.Id == challengeId);

        if (!challengeExists)
            return NotFound("Challenge was not found.");

        if (string.IsNullOrWhiteSpace(dto.MediaUrl))
            return BadRequest("Submission text or link is required.");

        var alreadySubmitted = await _context.ChallengeSubmissions
            .AnyAsync(s => s.ChallengeId == challengeId && s.UserId == userId);

        if (alreadySubmitted)
            return BadRequest("You already submitted for this challenge.");

        var submission = new ChallengeSubmission
        {
            ChallengeId = challengeId,
            UserId = userId,
            MediaUrl = dto.MediaUrl.Trim()
        };

        _context.ChallengeSubmissions.Add(submission);

        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
            user.Streak += 1;

        await _context.SaveChangesAsync();
        await NotifyChallengeFriends(submission.Id, userId, user?.FullName ?? "A friend");
        await _context.SaveChangesAsync();

        return Ok(ToSubmissionDto(submission, user?.FullName ?? string.Empty, 0));
    }

    [HttpPost("{challengeId}/submissions/photo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> SubmitPhoto(int challengeId, IFormFile file)
    {
        var userId = GetUserId();
        var challenge = await _context.Challenges.FindAsync(challengeId);

        if (challenge == null)
            return NotFound("Challenge was not found.");

        var alreadySubmitted = await _context.ChallengeSubmissions
            .AnyAsync(s => s.ChallengeId == challengeId && s.UserId == userId);

        if (alreadySubmitted)
            return BadRequest("You already submitted for this challenge.");

        var extensionResult = GetImageExtension(file);
        if (!extensionResult.IsValid)
            return BadRequest(extensionResult.ErrorMessage);

        var submission = new ChallengeSubmission
        {
            ChallengeId = challengeId,
            UserId = userId,
            MediaUrl = string.Empty
        };

        _context.ChallengeSubmissions.Add(submission);

        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
            user.Streak += 1;

        await _context.SaveChangesAsync();

        var uploadsFolder = Path.Combine(
            Directory.GetCurrentDirectory(),
            "uploads",
            "challenges");

        Directory.CreateDirectory(uploadsFolder);

        var storedPath = Path.Combine(uploadsFolder, $"{submission.Id}{extensionResult.Extension}");

        await using (var stream = System.IO.File.Create(storedPath))
        {
            await file.CopyToAsync(stream);
        }

        submission.MediaUrl = $"{Request.Scheme}://{Request.Host}/api/Challenges/submissions/{submission.Id}/content";

        // AI проверката е по желание — ако не е налична, снимката пак се запазва
        try
        {
            var evaluation = await _challengeVisionService.EvaluateAsync(
                challenge.Description,
                storedPath,
                file.ContentType,
                HttpContext.RequestAborted);

            submission.AiIsSatisfied = evaluation.IsSatisfied;
            submission.AiConfidence = evaluation.Confidence;
            submission.AiFeedback = evaluation.Feedback;
            submission.AiModel = evaluation.Model;
            submission.AiEvaluatedAt = evaluation.EvaluatedAt;
        }
        catch (Exception)
        {
            // AI не е налично — продължаваме без оценка
            submission.AiFeedback = null;
            submission.AiIsSatisfied = null;
        }

        await NotifyChallengeFriends(submission.Id, userId, user?.FullName ?? "A friend");
        await _context.SaveChangesAsync();

        return Ok(ToSubmissionDto(submission, user?.FullName ?? string.Empty, 0));
    }

    [HttpGet("submissions/{submissionId}/content")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSubmissionContent(int submissionId)
    {
        var exists = await _context.ChallengeSubmissions.AnyAsync(s => s.Id == submissionId);
        if (!exists)
            return NotFound();

        var uploadsFolder = Path.Combine(
            Directory.GetCurrentDirectory(),
            "uploads",
            "challenges");

        if (!Directory.Exists(uploadsFolder))
            return NotFound();

        var filePath = Directory
            .EnumerateFiles(uploadsFolder, $"{submissionId}.*")
            .FirstOrDefault();

        if (filePath == null)
            return NotFound();

        var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
        return File(bytes, GetContentType(Path.GetExtension(filePath)));
    }

    [HttpPost("submissions/{submissionId}/vote")]
    public async Task<IActionResult> Vote(int submissionId, [FromBody] CreateChallengeVoteDto dto)
    {
        var userId = GetUserId();
        var submission = await _context.ChallengeSubmissions.FindAsync(submissionId);

        if (submission == null)
            return NotFound("Submission was not found.");

        if (submission.UserId == userId)
            return BadRequest("You cannot vote for your own submission.");

        var canVote = await _context.Friends
            .AnyAsync(f => f.UserId == userId && f.FriendUserId == submission.UserId);

        if (!canVote)
            return Forbid();

        var vote = await _context.ChallengeVotes
            .FirstOrDefaultAsync(v => v.SubmissionId == submissionId && v.VotedByUserId == userId);

        if (vote == null)
        {
            vote = new ChallengeVote
            {
                SubmissionId = submissionId,
                VotedByUserId = userId
            };
            _context.ChallengeVotes.Add(vote);
        }

        vote.SelectedOption = string.IsNullOrWhiteSpace(dto.SelectedOption)
            ? "Guess"
            : dto.SelectedOption.Trim();

        await _context.SaveChangesAsync();
        return Ok("Vote saved.");
    }

    [HttpPost("admin/reset-active")]
    public async Task<IActionResult> ResetActiveChallenge()
    {
        var adminCheck = await RequireAdmin();
        if (adminCheck != null)
            return adminCheck;

        var today = DateTime.Today;
        var challenge = await _context.Challenges
            .Include(c => c.Submissions)
            .ThenInclude(s => s.Votes)
            .FirstOrDefaultAsync(c => c.Type == ChallengeType.Daily && c.StartDate == today);

        if (challenge == null)
            return Ok("No active challenge to reset.");

        var submissions = challenge.Submissions.ToList();
        var submissionIds = submissions.Select(s => s.Id).ToList();
        var votes = submissions.SelectMany(s => s.Votes).ToList();

        foreach (var submissionId in submissionIds)
            DeleteChallengeFile(submissionId);

        _context.ChallengeVotes.RemoveRange(votes);
        _context.ChallengeSubmissions.RemoveRange(submissions);
        await _context.SaveChangesAsync();

        return Ok($"Challenge reset. Removed {submissionIds.Count} submissions.");
    }

    private async Task<Challenge> GetOrCreateDailyChallenge()
    {
        var today = DateTime.Today;
        var challenge = await _context.Challenges
            .Where(c => c.Type == ChallengeType.Daily && c.StartDate == today)
            .FirstOrDefaultAsync();

        if (challenge != null)
            return challenge;

        var prompt = DailyPrompts[(today.DayOfYear - 1) % DailyPrompts.Length];
        challenge = new Challenge
        {
            Description = prompt,
            Type = ChallengeType.Daily,
            StartDate = today,
            RevealAt = today.AddHours(22)
        };

        _context.Challenges.Add(challenge);
        await _context.SaveChangesAsync();

        return challenge;
    }

    private async Task<HashSet<string>> GetFriendIds(string userId)
    {
        var friendIds = await _context.Friends
            .Where(f => f.UserId == userId)
            .Select(f => f.FriendUserId)
            .ToListAsync();

        return friendIds.ToHashSet();
    }

    private async Task NotifyChallengeFriends(int submissionId, string userId, string userName)
    {
        var friendIds = await GetFriendIds(userId);
        // NotifyChallengeFriends:
        _notificationService.AddForUsers(
            friendIds,
            "Ново снимка в предизвикателство",
            $"{userName} публикува снимка в предизвикателството.",
            NotificationType.ChallengeSubmission,
            "ChallengeSubmission",
            submissionId,
            userId);
    }

    private async Task<IActionResult?> RequireAdmin()
    {
        var user = await _userManager.FindByIdAsync(GetUserId());
        if (user == null)
            return Unauthorized();

        return await _userManager.IsInRoleAsync(user, AdminRole)
            ? null
            : Forbid();
    }

    private static ChallengeResponseDto ToDto(Challenge challenge) =>
        new()
        {
            Id = challenge.Id,
            Description = challenge.Description,
            Type = challenge.Type,
            StartDate = challenge.StartDate,
            RevealAt = challenge.RevealAt,
            IsRevealed = DateTime.Now >= challenge.RevealAt
        };

    private static ChallengeSubmissionResponseDto ToSubmissionDto(
        ChallengeSubmission submission,
        string userName,
        int voteCount) =>
        new()
        {
            Id = submission.Id,
            ChallengeId = submission.ChallengeId,
            UserName = userName,
            MediaUrl = submission.MediaUrl,
            SubmittedAt = submission.SubmittedAt,
            VoteCount = voteCount,
            AiIsSatisfied = submission.AiIsSatisfied,
            AiConfidence = submission.AiConfidence,
            AiFeedback = submission.AiFeedback,
            AiModel = submission.AiModel,
            AiEvaluatedAt = submission.AiEvaluatedAt
        };

    private static (bool IsValid, string Extension, string ErrorMessage) GetImageExtension(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return (false, string.Empty, "Photo file is required.");

        if (file.ContentType == null || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return (false, string.Empty, "Only image files are allowed.");

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

        return allowedExtensions.Contains(extension)
            ? (true, extension, string.Empty)
            : (false, string.Empty, "Allowed image types: jpg, jpeg, png, webp, gif.");
    }

    private static void DeleteChallengeFile(int submissionId)
    {
        var uploadsFolder = Path.Combine(
            Directory.GetCurrentDirectory(),
            "uploads",
            "challenges");

        if (!Directory.Exists(uploadsFolder))
            return;

        foreach (var filePath in Directory.EnumerateFiles(uploadsFolder, $"{submissionId}.*"))
        {
            try
            {
                System.IO.File.Delete(filePath);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private static string GetContentType(string extension) =>
        extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/jpeg"
        };
}
