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

        public DbSet<UserFavoritePodcasts> UserFavoritePodcasts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserFavoritePodcasts>()
                .HasKey(uf => new { uf.UserId, uf.PodcastId });

            modelBuilder.Entity<UserFavoritePodcasts>()
                .HasOne(uf => uf.User)
                .WithMany(u => u.UserFavorites)
                .HasForeignKey(uf => uf.UserId);

            modelBuilder.Entity<UserFavoritePodcasts>()
                .HasOne(uf => uf.Podcast)
                .WithMany(p => p.UserFavorites)
                .HasForeignKey(uf => uf.PodcastId);
        }
    }
}