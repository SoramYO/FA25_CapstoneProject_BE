using CusomMapOSM_Domain.Entities.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CusomMapOSM_Infrastructure.Databases.Configurations.SessionConfig;

internal class SessionMapStateConfiguration : IEntityTypeConfiguration<SessionMapState>
{
    public void Configure(EntityTypeBuilder<SessionMapState> builder)
    {
        builder.ToTable("session_map_states");

        builder.HasKey(sms => sms.SessionMapStateId);

        builder.Property(sms => sms.SessionMapStateId)
            .HasColumnName("session_map_state_id")
            .IsRequired();

        builder.Property(sms => sms.SessionId)
            .HasColumnName("session_id")
            .IsRequired();

        builder.Property(sms => sms.SessionQuestionId)
            .HasColumnName("session_question_id");

        builder.Property(sms => sms.Latitude)
            .HasColumnName("latitude")
            .HasColumnType("decimal(10,7)")
            .IsRequired();

        builder.Property(sms => sms.Longitude)
            .HasColumnName("longitude")
            .HasColumnType("decimal(10,7)")
            .IsRequired();

        builder.Property(sms => sms.ZoomLevel)
            .HasColumnName("zoom_level")
            .HasColumnType("decimal(4,2)")
            .IsRequired();

        builder.Property(sms => sms.Bearing)
            .HasColumnName("bearing")
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(0);

        builder.Property(sms => sms.Pitch)
            .HasColumnName("pitch")
            .HasColumnType("decimal(4,2)")
            .HasDefaultValue(0);

        builder.Property(sms => sms.TransitionDuration)
            .HasColumnName("transition_duration")
            .HasDefaultValue(1000);

        builder.Property(sms => sms.HighlightedLocationId)
            .HasColumnName("highlighted_location_id");

        builder.Property(sms => sms.HighlightedLayerId)
            .HasColumnName("highlighted_layer_id");

        builder.Property(sms => sms.IsLocked)
            .HasColumnName("is_locked")
            .HasDefaultValue(false);

        builder.Property(sms => sms.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relationships
        builder.HasOne(sms => sms.Session)
            .WithMany(s => s.SessionMapStates)
            .HasForeignKey(sms => sms.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sms => sms.SessionQuestion)
            .WithMany()
            .HasForeignKey(sms => sms.SessionQuestionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(sms => sms.HighlightedLocation)
            .WithMany()
            .HasForeignKey(sms => sms.HighlightedLocationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(sms => sms.HighlightedLayer)
            .WithMany()
            .HasForeignKey(sms => sms.HighlightedLayerId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(sms => sms.SessionId)
            .HasDatabaseName("IX_SessionMapState_SessionId");

        builder.HasIndex(sms => sms.SessionQuestionId)
            .HasDatabaseName("IX_SessionMapState_SessionQuestionId");

        // Composite index for real-time queries
        builder.HasIndex(sms => new { sms.SessionId, sms.CreatedAt })
            .HasDatabaseName("IX_SessionMapState_SessionId_CreatedAt");

        builder.HasIndex(sms => sms.IsLocked)
            .HasDatabaseName("IX_SessionMapState_IsLocked");
    }
}
