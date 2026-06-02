using MasterBeasProject.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MasterBeasProject.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<EngineerProfile> EngineerProfiles { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<PropertyDetails> PropertyDetails { get; set; }
        public DbSet<InspectionReport> InspectionReports { get; set; }
        public DbSet<ReportImage> ReportImages { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<EngineerAvailability> EngineerAvailabilities { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
            });

            builder.Entity<EngineerProfile>(entity =>
            {
                entity.HasOne(e => e.User)
                      .WithOne(u => u.EngineerProfile)
                      .HasForeignKey<EngineerProfile>(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.InspectionPrice).HasColumnType("decimal(10,2)");
                entity.Property(e => e.AverageRating).HasColumnType("decimal(3,2)");
            });

            builder.Entity<Booking>(entity =>
            {
                entity.HasOne(b => b.Client)
                      .WithMany(u => u.ClientBookings)
                      .HasForeignKey(b => b.ClientId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.EngineerProfile)
                      .WithMany(e => e.Bookings)
                      .HasForeignKey(b => b.EngineerProfileId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(b => b.Price).HasColumnType("decimal(10,2)");
                entity.Property(b => b.Latitude).HasColumnType("decimal(10,7)");
                entity.Property(b => b.Longitude).HasColumnType("decimal(10,7)");
            });

            builder.Entity<PropertyDetails>(entity =>
            {
                entity.HasOne(p => p.Booking)
                      .WithOne(b => b.PropertyDetails)
                      .HasForeignKey<PropertyDetails>(p => p.BookingId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<InspectionReport>(entity =>
            {
                entity.HasOne(r => r.Booking)
                      .WithOne(b => b.InspectionReport)
                      .HasForeignKey<InspectionReport>(r => r.BookingId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ReportImage>(entity =>
            {
                entity.HasOne(i => i.InspectionReport)
                      .WithMany(r => r.Images)
                      .HasForeignKey(i => i.InspectionReportId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Review>(entity =>
            {
                entity.HasOne(r => r.Booking)
                      .WithOne(b => b.Review)
                      .HasForeignKey<Review>(r => r.BookingId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Client)
                      .WithMany()
                      .HasForeignKey(r => r.ClientId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.EngineerProfile)
                      .WithMany(e => e.Reviews)
                      .HasForeignKey(r => r.EngineerProfileId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ChatMessage>(entity =>
            {
                entity.HasOne(c => c.Booking)
                      .WithMany(b => b.ChatMessages)
                      .HasForeignKey(c => c.BookingId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.Sender)
                      .WithMany()
                      .HasForeignKey(c => c.SenderId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Notification>(entity =>
            {
                entity.HasOne(n => n.User)
                      .WithMany(u => u.Notifications)
                      .HasForeignKey(n => n.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}