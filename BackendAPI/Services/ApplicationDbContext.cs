using BackendAPI.Models.Hall;
using BackendAPI.Models.Movie;
using BackendAPI.Models.Newsletter;
using BackendAPI.Models.Order;
using BackendAPI.Models.Screening;
using BackendAPI.Models.Seat;
using BackendAPI.Models.Tariff;
using BackendAPI.Models.User;
using Microsoft.EntityFrameworkCore;

namespace API.Services
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<HallModel> Halls { get; set; }
        public DbSet<MovieModel> Movies { get; set; }
        public DbSet<OrderModel> Orders { get; set; }
        public DbSet<ScreeningModel> Screenings { get; set; }
        public DbSet<SeatModel> Seats { get; set; }
        public DbSet<TariffModel> Tariffs { get; set; }
        public DbSet<UserModel> Users { get; set; }
        public DbSet<NewsletterSubscriberModel> NewsletterSubscribers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<OrderModel>()
                .HasOne(o => o.Seat)
                .WithMany()
                .HasForeignKey(o => o.SeatId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderModel>()
                .HasOne(o => o.Screening)
                .WithMany()
                .HasForeignKey(o => o.ScreeningId)
                .OnDelete(DeleteBehavior.Restrict); // of NoAction

            modelBuilder.Entity<OrderModel>()
                .HasIndex(o => new { o.ScreeningId, o.SeatId })
                .IsUnique();
        }
    }
}