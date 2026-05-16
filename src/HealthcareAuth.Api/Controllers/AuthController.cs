using System.Security.Claims;
using HealthcareAuth.Api.Contracts;
using HealthcareAuth.Api.Models;
using HealthcareAuth.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthcareAuth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuditService _auditService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtTokenService,
        IAuditService auditService)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _auditService = auditService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var token = await _jwtTokenService.CreateTokenAsync(user);
        await _auditService.WriteAsync("Login", nameof(ApplicationUser), user.Id, "User logged in.");

        return Ok(token);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileResponse>> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = userId is null ? null : await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Unauthorized();
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserProfileResponse(user.Id, user.Email ?? string.Empty, user.FullName, user.Department, roles.ToList()));
    }

    [HttpPost("users")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<UserProfileResponse>> RegisterUser(RegisterUserRequest request)
    {
        if (!AppRoles.All.Contains(request.Role))
        {
            return BadRequest(new { message = "Role is not supported." });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            FullName = request.FullName,
            Department = request.Department
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.Select(x => x.Description));
        }

        await _userManager.AddToRoleAsync(user, request.Role);
        await _auditService.WriteAsync("CreateUser", nameof(ApplicationUser), user.Id, $"Created user {request.Email} with role {request.Role}.");

        return CreatedAtAction(nameof(Me), new UserProfileResponse(user.Id, user.Email ?? string.Empty, user.FullName, user.Department, [request.Role]));
    }
}
