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
public class ChallengesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;

    private static readonly string[] DailyPrompts =
    {
        "Capture something green",
        "Capture something shiny",
        "Capture a favorite detail from today",
        "Capture something that made you smile"
    };

    public ChallengesController(AppDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
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
        var submissions = await _context.ChallengeSubmissions
            .Where(s => s.ChallengeId == challengeId)
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
                VoteCount = s.Votes.Count
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

        return Ok(new ChallengeSubmissionResponseDto
        {
            Id = submission.Id,
            ChallengeId = submission.ChallengeId,
            UserName = user?.FullName ?? string.Empty,
            MediaUrl = submission.MediaUrl,
            SubmittedAt = submission.SubmittedAt,
            VoteCount = 0
        });
    }

    [HttpPost("{challengeId}/submissions/photo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> SubmitPhoto(int challengeId, IFormFile file)
    {
        var userId = GetUserId();
        var challengeExists = await _context.Challenges.AnyAsync(c => c.Id == challengeId);

        if (!challengeExists)
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
        await _context.SaveChangesAsync();

        return Ok(new ChallengeSubmissionResponseDto
        {
            Id = submission.Id,
            ChallengeId = submission.ChallengeId,
            UserName = user?.FullName ?? string.Empty,
            MediaUrl = submission.MediaUrl,
            SubmittedAt = submission.SubmittedAt,
            VoteCount = 0
        });
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

    private async Task<Challenge> GetOrCreateDailyChallenge()
    {
        var today = DateTime.UtcNow.Date;
        var challenge = await _context.Challenges
            .Where(c => c.Type == ChallengeType.Daily && c.StartDate == today)
            .FirstOrDefaultAsync();

        if (challenge != null)
            return challenge;

        var prompt = DailyPrompts[Math.Abs(today.DayOfYear) % DailyPrompts.Length];
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

    private static ChallengeResponseDto ToDto(Challenge challenge) =>
        new()
        {
            Id = challenge.Id,
            Description = challenge.Description,
            Type = challenge.Type,
            StartDate = challenge.StartDate,
            RevealAt = challenge.RevealAt,
            IsRevealed = DateTime.UtcNow >= challenge.RevealAt
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

    private static string GetContentType(string extension) =>
        extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/jpeg"
        };
}
