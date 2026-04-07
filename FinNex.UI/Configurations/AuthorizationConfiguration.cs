using FinNex.Domain;
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
                    policy.RequireRole(RoleNames.Admin, RoleNames.HR, RoleNames.HR_View, RoleNames.Rehber));

                options.AddPolicy(PolicyNames.HR_Full, policy =>
                    policy.RequireRole(RoleNames.HR, RoleNames.Admin));

                options.AddPolicy(PolicyNames.Admin_Full, policy =>
                    policy.RequireRole(RoleNames.Admin));
            });

            return services;
        }
    }
}