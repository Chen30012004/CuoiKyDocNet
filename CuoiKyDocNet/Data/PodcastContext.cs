using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CuoiKyDocNet.Models;

namespace CuoiKyDocNet.Data
{
    public class PodcastContext : IdentityDbContext<ApplicationUser>
    {
        public PodcastContext(DbContextOptions<PodcastContext> options)
            : base(options)
        {
        }

        public DbSet<Podcast> Podcasts { get; set; }
        public DbSet<Episode> Episodes { get; set; }
        public DbSet<UserFavoritePodcast> UserFavoritePodcasts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình quan hệ nhiều-nhiều
            modelBuilder.Entity<UserFavoritePodcast>()
                .HasKey(ufp => new { ufp.UserId, ufp.PodcastId });

            modelBuilder.Entity<UserFavoritePodcast>()
                .HasOne(ufp => ufp.User)
                .WithMany(u => u.FavoritePodcasts)
                .HasForeignKey(ufp => ufp.UserId);

            modelBuilder.Entity<UserFavoritePodcast>()
                .HasOne(ufp => ufp.Podcast)
                .WithMany()
                .HasForeignKey(ufp => ufp.PodcastId);
        }
    }
}