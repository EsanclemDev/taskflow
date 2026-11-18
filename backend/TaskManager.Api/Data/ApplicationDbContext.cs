using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Models;

namespace TaskManager.Api.Data;

public class ApplicationDbContext :
    IdentityDbContext<ApplicationUser>,
    IDataProtectionKeyContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    public DbSet<RefreshSession> RefreshSessions
        => Set<RefreshSession>();

    public DbSet<DataProtectionKey> DataProtectionKeys
        => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<TaskItem>(entity =>
        {
            entity.HasOne(task => task.User)
                .WithMany()
                .HasForeignKey(task => task.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(task => task.Status)
                .HasMaxLength(20)
                .HasDefaultValue(TaskWorkflowStatus.Pending)
                .IsRequired();

            entity.Property(task => task.Priority)
                .HasMaxLength(10)
                .HasDefaultValue(TaskPriority.Medium)
                .IsRequired();

            entity.Property(task => task.Category)
                .HasMaxLength(20)
                .HasDefaultValue(TaskCategory.Other)
                .IsRequired();

            entity.Property(task => task.DueDate)
                .HasColumnType("date");

            entity.Property(task => task.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entity.HasOne(task => task.CreatedByUser)
                .WithMany()
                .HasForeignKey(task => task.CreatedByUserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(task => task.AssignedToUser)
                .WithMany()
                .HasForeignKey(task => task.AssignedToUserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(task => task.CreatedByUserId);
            entity.HasIndex(task => task.AssignedToUserId);
            entity.HasIndex(task => task.DueDate);
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.IsDemo)
                .HasDefaultValue(false)
                .IsRequired();

            entity.HasIndex(user => user.DemoExpiresAt);
        });

        // Se conserva sin cambios la configuración de sesiones.
        builder.Entity<RefreshSession>(entity =>
        {
            entity.HasKey(session => session.Id);

            entity.HasOne(session => session.User)
                .WithMany()
                .HasForeignKey(session => session.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(session => session.TokenHash)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(session => session.FamilyId)
                .IsRequired();

            entity.HasIndex(session => session.TokenHash)
                .IsUnique();

            entity.HasIndex(session => session.UserId);

            entity.Property(session => session.RevocationReason)
                .HasMaxLength(32);

            entity.HasIndex(session => session.FamilyId);
        });
    }
}
