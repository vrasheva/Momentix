namespace Momentix.API.Models
{
    public class ChallengeSubmission
    {
        public int Id { get; set; }
        public int ChallengeId { get; set; }
        public Challenge Challenge { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;

        public string MediaUrl { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ChallengeVote> Votes { get; set; } = new List<ChallengeVote>();
    }
}