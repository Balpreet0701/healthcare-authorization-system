using HealthcareAuth.Api.Contracts;
using HealthcareAuth.Api.Models;

namespace HealthcareAuth.Api.Services;

public interface IJwtTokenService
{
    Task<AuthResponse> CreateTokenAsync(ApplicationUser user);
}
