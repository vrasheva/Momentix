namespace Momentix.API.Models
{
    public enum ChallengeType { Daily, Weekly }

    public class Challenge
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public ChallengeType Type { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime RevealAt { get; set; }

        public ICollection<ChallengeSubmission> Submissions { get; set; } = new List<ChallengeSubmission>();
    }
}