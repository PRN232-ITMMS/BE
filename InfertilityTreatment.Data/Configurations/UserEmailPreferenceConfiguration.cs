using InfertilityTreatment.Entity.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Data.Configurations
{
    public class UserEmailPreferenceConfiguration : IEntityTypeConfiguration<UserEmailPreference>
    {
        public void Configure(EntityTypeBuilder<UserEmailPreference> builder)
        {
            builder.ToTable("UserEmailPreferences");

            builder.HasKey(uep => uep.Id);

            builder.Property(uep => uep.UserId)
                   .IsRequired();

            builder.Property(uep => uep.NotificationType)
                   .HasConversion<string>()
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(uep => uep.IsEnabled)
                   .IsRequired();

            builder.HasIndex(uep => new { uep.UserId, uep.NotificationType })
                   .IsUnique();

            builder.HasOne(uep => uep.User)
                   .WithMany(u => u.UserEmailPreferences)
                   .HasForeignKey(uep => uep.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
