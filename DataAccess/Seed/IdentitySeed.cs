using FinNex.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace FinNex.DataAccess.Seed
{
    public static class IdentitySeed
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            // ======================
            // 1. Rollar
            // ======================
            string[] roles = { "Admin", "Operator", "Viewer" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new AppRole { Name = role });
                }
            }

            // ======================
            // 2. Admin user
            // ======================
            var adminEmail = "admin@finnex.local";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new AppUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    EmailConfirmed = true,

                    Ad = "System",
                    Soyad = "Administrator",
                    Aktivdir = true,
                    QeydiyyatTarixi = DateTime.Now
                };


                var result = await userManager.CreateAsync(adminUser, "Admin@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}
