using InfertilityTreatment.Entity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            builder.HasIndex(x => new { x.CycleId, x.PhaseOrder })
                   .IsUnique();
        }
    
    }
}
