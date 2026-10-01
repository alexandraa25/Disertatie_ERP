using Microsoft.AspNetCore.Authorization;

namespace ERPSystem.Configuration;

public static class AuthorizationConfiguration
{
    public static void ConfigureApiAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
    }
}
