using HealthcareAuth.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HealthcareAuth.Api.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<AuthorizationRequest> AuthorizationRequests => Set<AuthorizationRequest>();
    public DbSet<MedicalDocument> MedicalDocuments => Set<MedicalDocument>();
    public DbSet<UrlAttachment> UrlAttachments => Set<UrlAttachment>();
    public DbSet<AuthorizationStatusHistory> AuthorizationStatusHistory => Set<AuthorizationStatusHistory>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Patient>(entity =>
        {
            entity.HasIndex(x => x.MedicalRecordNumber).IsUnique();
            entity.Property(x => x.MedicalRecordNumber).HasMaxLength(32);
            entity.Property(x => x.FirstName).HasMaxLength(100);
            entity.Property(x => x.LastName).HasMaxLength(100);
            entity.Property(x => x.Gender).HasMaxLength(40);
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.InsuranceProvider).HasMaxLength(160);
            entity.Property(x => x.MemberNumber).HasMaxLength(80);
        });

        builder.Entity<AuthorizationRequest>(entity =>
        {
            entity.HasIndex(x => x.RequestNumber).IsUnique();
            entity.Property(x => x.RequestNumber).HasMaxLength(40);
            entity.Property(x => x.RequestedService).HasMaxLength(240);
            entity.Property(x => x.DiagnosisCode).HasMaxLength(40);
            entity.Property(x => x.ProcedureCode).HasMaxLength(40);
            entity.Property(x => x.Priority).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.AiRecommendation).HasMaxLength(80);
            entity.Property(x => x.AiConfidenceScore).HasColumnType("decimal(5,2)");

            entity
                .HasOne(x => x.Patient)
                .WithMany(x => x.AuthorizationRequests)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne(x => x.CreatedBy)
                .WithMany()
                .HasForeignKey(x => x.CreatedById)
                .OnDelete(DeleteBehavior.SetNull);

            entity
                .HasOne(x => x.AssignedReviewer)
                .WithMany()
                .HasForeignKey(x => x.AssignedReviewerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<MedicalDocument>(entity =>
        {
            entity.Property(x => x.FileName).HasMaxLength(260);
            entity.Property(x => x.StoredFileName).HasMaxLength(260);
            entity.Property(x => x.ContentType).HasMaxLength(120);
            entity.Property(x => x.FilePath).HasMaxLength(600);
            entity.Property(x => x.OcrStatus).HasConversion<string>().HasMaxLength(40);

            entity
                .HasOne(x => x.AuthorizationRequest)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.AuthorizationRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UrlAttachment>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(200);
            entity.Property(x => x.Url).HasMaxLength(1200);
            entity
                .HasOne(x => x.AuthorizationRequest)
                .WithMany(x => x.UrlAttachments)
                .HasForeignKey(x => x.AuthorizationRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuthorizationStatusHistory>(entity =>
        {
            entity.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.Reason).HasMaxLength(1000);
            entity
                .HasOne(x => x.AuthorizationRequest)
                .WithMany(x => x.StatusHistory)
                .HasForeignKey(x => x.AuthorizationRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(x => x.CreatedAt);
            entity.Property(x => x.Action).HasMaxLength(120);
            entity.Property(x => x.EntityName).HasMaxLength(120);
            entity.Property(x => x.EntityId).HasMaxLength(80);
            entity.Property(x => x.UserName).HasMaxLength(256);
            entity.Property(x => x.IpAddress).HasMaxLength(80);
        });

        builder.Entity<Notification>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
            entity.Property(x => x.Title).HasMaxLength(180);
            entity.Property(x => x.Link).HasMaxLength(600);
            entity
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
