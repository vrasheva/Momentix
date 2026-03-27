namespace Momentix.API.Models
{
    public class ChallengeVote
    {
        public int Id { get; set; }
        public int SubmissionId { get; set; }
        public ChallengeSubmission Submission { get; set; } = null!;

        public string VotedByUserId { get; set; } = string.Empty;
        public User VotedBy { get; set; } = null!;

        public string SelectedOption { get; set; } = string.Empty;
    }
}