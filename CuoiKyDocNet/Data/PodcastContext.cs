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
        public DbSet<UserFavoritePodcast> UserFavoritePodcasts { get; set; }
        public DbSet<Episode> Episodes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<UserFavoritePodcast>()
                .HasKey(ufp => new { ufp.UserId, ufp.PodcastId });

            builder.Entity<UserFavoritePodcast>()
                .HasOne(ufp => ufp.User)
                .WithMany(u => u.FavoritePodcasts)
                .HasForeignKey(ufp => ufp.UserId);

            builder.Entity<UserFavoritePodcast>()
                .HasOne(ufp => ufp.Podcast)
                .WithMany(p => p.UserFavorites)
                .HasForeignKey(ufp => ufp.PodcastId);

            builder.Entity<Episode>()
                .HasOne(e => e.Podcast)
                .WithMany(p => p.Episodes)
                .HasForeignKey(e => e.PodcastId);
        }
    }
}