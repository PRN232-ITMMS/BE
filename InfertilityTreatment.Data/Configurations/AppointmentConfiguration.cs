using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InfertilityTreatment.Entity.Entities;

namespace InfertilityTreatment.Data.Configurations
{
    public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
    {
        public void Configure(EntityTypeBuilder<Appointment> builder)
        {
            builder.ToTable("Appointments");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.CycleId)
                   .IsRequired(false);

            builder.Property(a => a.DoctorId)
                   .IsRequired();

            builder.Property(a => a.AppointmentType)
                   .IsRequired()
                   .HasConversion<byte>(); 

            builder.Property(a => a.ScheduledDateTime)
                   .IsRequired();

            builder.Property(a => a.Status)
                   .IsRequired()
                   .HasConversion<byte>(); 

            //builder.Property(a => a.Duration)
            //       .IsRequired();

            builder.Property(a => a.Notes)
                   .HasColumnType("nvarchar(max)");

            builder.Property(a => a.Results)
                   .HasColumnType("nvarchar(max)");

            builder.Property(a => a.CreatedAt)
                   .IsRequired();

            builder.Property(a => a.UpdatedAt)
                   .IsRequired();

            // Relationships
            builder.HasOne(a => a.TreatmentCycle)
                   .WithMany(c => c.Appointments) // Ensure TreatmentCycle has ICollection<Appointment> Appointments
                   .HasForeignKey(a => a.CycleId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.Doctor)
                   .WithMany(d => d.Appointments) // Ensure Doctor has ICollection<Appointment> Appointments
                   .HasForeignKey(a => a.DoctorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(a => a.DoctorId);
            builder.HasIndex(a => a.ScheduledDateTime);
        }
    }
}