using Hangfire.Dashboard;
using HealthcareAuth.Api.Models;

namespace HealthcareAuth.Api.Infrastructure;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
            && (httpContext.User.IsInRole(AppRoles.Admin) || httpContext.User.IsInRole(AppRoles.Reviewer));
    }
}
