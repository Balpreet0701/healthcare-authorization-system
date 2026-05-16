using HealthcareAuth.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HealthcareAuth.Api.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        await db.Database.EnsureCreatedAsync();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var admin = await EnsureUserAsync(
            userManager,
            "admin@healthauth.local",
            "Admin User",
            "Platform Administration",
            "Admin@12345",
            AppRoles.Admin);

        var reviewer = await EnsureUserAsync(
            userManager,
            "reviewer@healthauth.local",
            "Clinical Reviewer",
            "Utilization Management",
            "Reviewer@12345",
            AppRoles.Reviewer);

        var intake = await EnsureUserAsync(
            userManager,
            "intake@healthauth.local",
            "Intake Coordinator",
            "Prior Authorization",
            "Intake@12345",
            AppRoles.Intake);

        if (!await db.Patients.AnyAsync())
        {
            var patient = new Patient
            {
                MedicalRecordNumber = "MRN-100245",
                FirstName = "Avery",
                LastName = "Johnson",
                DateOfBirth = new DateOnly(1981, 4, 18),
                Gender = "Female",
                Phone = "555-0184",
                Email = "avery.johnson@example.com",
                InsuranceProvider = "Contoso Health",
                MemberNumber = "CH-8842001"
            };

            db.Patients.Add(patient);
            await db.SaveChangesAsync();

            db.AuthorizationRequests.Add(new AuthorizationRequest
            {
                RequestNumber = CreateRequestNumber(),
                PatientId = patient.Id,
                RequestedService = "MRI lumbar spine without contrast",
                DiagnosisCode = "M54.50",
                ProcedureCode = "72148",
                Priority = PriorityLevel.Urgent,
                Status = AuthorizationStatus.Submitted,
                ClinicalNotes = "Persistent lower back pain for 8 weeks with radiculopathy into left leg. Conservative therapy, NSAIDs, and physical therapy completed without sustained relief.",
                CreatedById = intake.Id,
                AssignedReviewerId = reviewer.Id,
                SubmittedAt = DateTime.UtcNow.AddHours(-6),
                DueDate = DateTime.UtcNow.AddDays(2)
            });

            await db.AuditLogs.AddAsync(new AuditLog
            {
                UserId = admin.Id,
                UserName = admin.Email ?? "admin",
                Action = "Seed",
                EntityName = nameof(AuthorizationRequest),
                EntityId = "sample",
                Details = "Created sample patient and authorization request."
            });

            await db.SaveChangesAsync();
        }

        logger.LogInformation("Database initialization complete. Seed accounts: admin@healthauth.local, reviewer@healthauth.local, intake@healthauth.local");
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string fullName,
        string department,
        string password,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                Department = department
            };

            var created = await userManager.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                var errors = string.Join("; ", created.Errors.Select(x => x.Description));
                throw new InvalidOperationException($"Could not seed user {email}: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }

    public static string CreateRequestNumber()
    {
        return $"AUTH-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
    }
}
