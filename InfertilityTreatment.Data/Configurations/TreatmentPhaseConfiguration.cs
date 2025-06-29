using InfertilityTreatment.Entity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InfertilityTreatment.Data.Configurations
{
    public class TreatmentPhaseConfiguration : IEntityTypeConfiguration<TreatmentPhase>
    {
        public void Configure(EntityTypeBuilder<TreatmentPhase> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.PhaseName)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(x => x.Cost)
                   .HasColumnType("decimal(12,2)");

            builder.HasIndex(x => new { x.CycleId, x.PhaseOrder });
            builder.HasIndex(x => x.CycleId);
            builder.HasIndex(x => x.PhaseOrder);


            builder.HasMany(tp => tp.Prescriptions)
                   .WithOne(p => p.TreatmentPhase)
                   .HasForeignKey(p => p.PhaseId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
