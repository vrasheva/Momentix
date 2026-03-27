namespace Momentix.API.Models
{
    public class TimeCapsuleMember
    {
        public int Id { get; set; }
        public int TimeCapsuleId { get; set; }
        public TimeCapsule TimeCapsule { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;
    }
}