namespace HealthcareAuth.Api.Models;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Intake = "Intake";
    public const string Reviewer = "Reviewer";

    public static readonly string[] All = [Admin, Intake, Reviewer];
}
