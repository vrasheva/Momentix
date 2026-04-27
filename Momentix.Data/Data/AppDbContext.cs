using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Momentix.Data.Models;

namespace Momentix.Data.Data
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Album> Albums { get; set; }
        public DbSet<AlbumMember> AlbumMembers { get; set; }
        public DbSet<Media> MediaItems { get; set; }
        public DbSet<TimeCapsule> TimeCapsules { get; set; }
        public DbSet<TimeCapsuleMember> TimeCapsuleMembers { get; set; }
        public DbSet<Challenge> Challenges { get; set; }
        public DbSet<ChallengeSubmission> ChallengeSubmissions { get; set; }
        public DbSet<ChallengeVote> ChallengeVotes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<AlbumMember>()
                .HasIndex(am => new { am.AlbumId, am.UserId })
                .IsUnique();

            builder.Entity<AlbumMember>()
                .HasOne(am => am.User)
                .WithMany(u => u.AlbumMemberships)
                .HasForeignKey(am => am.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<TimeCapsuleMember>()
                .HasIndex(tm => new { tm.TimeCapsuleId, tm.UserId })
                .IsUnique();

            builder.Entity<TimeCapsuleMember>()
                .HasOne(tm => tm.User)
                .WithMany()
                .HasForeignKey(tm => tm.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ChallengeVote>()
                .HasIndex(cv => new { cv.SubmissionId, cv.VotedByUserId })
                .IsUnique();

            builder.Entity<ChallengeVote>()
                .HasOne(cv => cv.VotedBy)
                .WithMany()
                .HasForeignKey(cv => cv.VotedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Media>()
                .HasOne(m => m.UploadedBy)
                .WithMany()
                .HasForeignKey(m => m.UploadedById)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<TimeCapsule>()
                .HasOne(tc => tc.Owner)
                .WithMany(u => u.TimeCapsules)
                .HasForeignKey(tc => tc.OwnerId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ChallengeSubmission>()
                .HasOne(cs => cs.User)
                .WithMany()
                .HasForeignKey(cs => cs.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}