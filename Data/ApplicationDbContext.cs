using carrentalmvc.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace carrentalmvc.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Brand> Brands { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Car> Cars { get; set; }
        public DbSet<CarImage> CarImages { get; set; }
        public DbSet<CarModel3D> CarModel3Ds { get; set; }
        public DbSet<Feature> Features { get; set; }
        public DbSet<CarFeature> CarFeatures { get; set; }
        public DbSet<Rental> Rentals { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<CarModel3D> CarModels3D { get; set; }
        public DbSet<Model3DTemplate> Model3DTemplates { get; set; }
        public DbSet<DriverRequest> DriverRequests { get; set; }
        public DbSet<DriverAssignment> DriverAssignments { get; set; }
        public DbSet<CarOwnerRequest> CarOwnerRequests { get; set; }
        public DbSet<PaymentDistribution> PaymentDistributions { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Composite key cho CarFeature
            modelBuilder.Entity<CarFeature>()
                .HasKey(cf => new { cf.CarId, cf.FeatureId });

            // =========================
            // Model3DTemplate - Cấu hình primary key
            // =========================
            modelBuilder.Entity<Model3DTemplate>()
                .HasKey(t => t.TemplateId);

            // =========================
            // Car relationships
            // =========================
            modelBuilder.Entity<Car>()
                .HasOne(c => c.Owner)
                .WithMany(u => u.Cars)
                .HasForeignKey(c => c.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Car>()
                .HasOne(c => c.Brand)
                .WithMany(b => b.Cars)
                .HasForeignKey(c => c.BrandId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Car>()
                .HasOne(c => c.Category)
                .WithMany(cat => cat.Cars)
                .HasForeignKey(c => c.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================
            // Rentals
            // =========================
            modelBuilder.Entity<Rental>()
                .HasOne(r => r.Car)
                .WithMany(c => c.Rentals)
                .HasForeignKey(r => r.CarId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Rental>()
                .HasOne(r => r.Renter)
                .WithMany(u => u.Rentals)
                .HasForeignKey(r => r.RenterId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================
            // Payments
            // =========================
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Rental)
                .WithMany(r => r.Payments)
                .HasForeignKey(p => p.RentalId)
                .OnDelete(DeleteBehavior.Cascade);

            // =========================
            // CarImages
            // =========================
            modelBuilder.Entity<CarImage>()
                .HasOne(ci => ci.Car)
                .WithMany(c => c.CarImages)
                .HasForeignKey(ci => ci.CarId)
                .OnDelete(DeleteBehavior.Cascade);

            // =========================
            // CarModel3D
            // =========================
            modelBuilder.Entity<CarModel3D>()
                .HasOne(cm => cm.Car)
                .WithOne(c => c.CarModel3D)
                .HasForeignKey<CarModel3D>(cm => cm.CarId)
                .OnDelete(DeleteBehavior.Cascade);

            // =========================
            // Model3DTemplate relationships
            // =========================
            modelBuilder.Entity<Model3DTemplate>()
                .HasOne(t => t.Brand)
                .WithMany()
                .HasForeignKey(t => t.BrandId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Model3DTemplate>()
                .HasOne(t => t.Category)
                .WithMany()
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // =========================
            // Reviews
            // =========================
            modelBuilder.Entity<Review>()
                .HasOne(rv => rv.Car)
                .WithMany(c => c.Reviews)
                .HasForeignKey(rv => rv.CarId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(rv => rv.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(rv => rv.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ✅ THÊM: Configure DriverRequest
            modelBuilder.Entity<DriverRequest>(entity =>
            {
                entity.HasKey(dr => dr.DriverRequestId);

                entity.HasOne(dr => dr.User)
                    .WithMany()
                    .HasForeignKey(dr => dr.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(dr => dr.CarOwner)
                    .WithMany()
                    .HasForeignKey(dr => dr.CarOwnerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(dr => dr.Processor)
                    .WithMany()
                    .HasForeignKey(dr => dr.ProcessedBy)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(dr => dr.UserId);
                entity.HasIndex(dr => dr.CarOwnerId);
                entity.HasIndex(dr => dr.Status);
            });

            // ✅ THÊM: Configure DriverAssignment
            modelBuilder.Entity<DriverAssignment>(entity =>
            {
                entity.HasKey(da => da.DriverAssignmentId);

                // ❌ REMOVED: Salary field precision (fields no longer exist)
                entity.Property(da => da.DailyDriverFee)
                    .HasPrecision(18, 2)
                    .IsRequired();

                entity.HasOne(da => da.CarOwner)
                    .WithMany(u => u.ManagedDrivers)
                    .HasForeignKey(da => da.CarOwnerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(da => da.Driver)
                    .WithMany(u => u.DriverAssignments)
                    .HasForeignKey(da => da.DriverId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(da => da.CarOwnerId);
                entity.HasIndex(da => da.DriverId);
            });

            // ✅ Configure CarOwnerRequest
            modelBuilder.Entity<CarOwnerRequest>(entity =>
            {
                entity.HasKey(cor => cor.CarOwnerRequestId);

                entity.HasOne(cor => cor.User)
                    .WithMany()
                    .HasForeignKey(cor => cor.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(cor => cor.Processor)
                    .WithMany()
                    .HasForeignKey(cor => cor.ProcessedBy)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(cor => cor.UserId);
                entity.HasIndex(cor => cor.Status);
            });

            // ✅ THÊM: Configure Rental.Driver
            modelBuilder.Entity<Rental>()
                .HasOne(r => r.Driver)
                .WithMany(u => u.DriverRentals)
                .HasForeignKey(r => r.DriverId)
                .OnDelete(DeleteBehavior.SetNull);

            // ✅ THÊM: Configure PaymentDistribution
            modelBuilder.Entity<PaymentDistribution>(entity =>
            {
                entity.HasKey(pd => pd.PaymentDistributionId);

                entity.Property(pd => pd.Amount)
                    .HasPrecision(18, 2)
                    .IsRequired();

                entity.Property(pd => pd.RecipientId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.HasOne(pd => pd.Payment)
                    .WithMany()
                    .HasForeignKey(pd => pd.PaymentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pd => pd.Recipient)
                    .WithMany()
                    .HasForeignKey(pd => pd.RecipientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(pd => pd.PaymentId);
                entity.HasIndex(pd => pd.RecipientId);
                entity.HasIndex(pd => pd.Status);
            });
        }
    }
}
