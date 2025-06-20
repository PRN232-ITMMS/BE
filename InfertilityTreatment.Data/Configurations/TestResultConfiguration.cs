using InfertilityTreatment.Entity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InfertilityTreatment.Data.Configurations
{
    public class TestResultConfiguration : IEntityTypeConfiguration<TestResult>
    {
        public void Configure(EntityTypeBuilder<TestResult> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TestType)
                   .HasConversion<byte>()
                   .IsRequired();

            builder.Property(x => x.Status)
                   .HasConversion<byte>()
                   .HasColumnType("nvarchar(50)");

            builder.Property(x => x.TestDate)
                   .IsRequired();

            builder.Property(x => x.Results)
                   .HasColumnType("nvarchar(max)");

            builder.Property(x => x.ReferenceRange)
                   .HasColumnType("nvarchar(100)");

            builder.Property(x => x.DoctorNotes)
                   .HasColumnType("nvarchar(max)");

            builder.HasOne(x => x.TreatmentCycle)
                   .WithMany(x => x.TestResults)
                   .HasForeignKey(x => x.CycleId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
