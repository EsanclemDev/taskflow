
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

        // Tareas y usuarios.
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
        });

        // Sesiones de autenticación.
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
