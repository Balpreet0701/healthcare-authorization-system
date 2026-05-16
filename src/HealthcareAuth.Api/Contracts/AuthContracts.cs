namespace HealthcareAuth.Api.Contracts;

public record LoginRequest(string Email, string Password);

public record RegisterUserRequest(
    string Email,
    string FullName,
    string Department,
    string Password,
    string Role);

public record UserProfileResponse(
    string Id,
    string Email,
    string FullName,
    string Department,
    IReadOnlyCollection<string> Roles);

public record AuthResponse(
    string Token,
    DateTime ExpiresAt,
    UserProfileResponse User);
