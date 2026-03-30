using Microsoft.AspNetCore.Authorization;

namespace FinNex.UI.Configurations
{
    public static class AuthorizationConfiguration
    {
        public static IServiceCollection AddAppAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy(PolicyNames.HR_View, policy =>
                    policy.RequireRole("HR", "HR_View"));

                options.AddPolicy(PolicyNames.HR_Full, policy =>
                    policy.RequireRole("HR"));
            });

            return services;
        }
    }
}