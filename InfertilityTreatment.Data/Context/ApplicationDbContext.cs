using Microsoft.EntityFrameworkCore;
using InfertilityTreatment.Entity.Entities;
using InfertilityTreatment.Entity.Common;

namespace InfertilityTreatment.Data.Context
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // User Management DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        // Treatment DbSets
        public DbSet<TreatmentService> TreatmentServices { get; set; }
        public DbSet<TreatmentPackage> TreatmentPackages { get; set; }
        public DbSet<TreatmentCycle> TreatmentCycles { get; set; }
        //public DbSet<TreatmentPhase> TreatmentPhases { get; set; }

        // Appointment & Monitoring DbSets
        //public DbSet<Appointment> Appointments { get; set; }
        //public DbSet<TestResult> TestResults { get; set; }

        // Medication DbSets
        //public DbSet<Medication> Medications { get; set; }
        //public DbSet<Prescription> Prescriptions { get; set; }

        // Content & Feedback DbSets
        //public DbSet<BlogPost> BlogPosts { get; set; }
        //public DbSet<Review> Reviews { get; set; }
        //public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Apply all entity configurations from assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Update audit fields automatically
            var entries = ChangeTracker.Entries<BaseEntity>();

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}