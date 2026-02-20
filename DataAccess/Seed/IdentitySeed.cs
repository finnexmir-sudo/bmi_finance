using FinNex.DataAccess.Contexts;
using FinNex.Domain;
using FinNex.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

            // ======================
            // 3. Valyutalar
            // ======================

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (!await context.Set<Valyuta>().AnyAsync())
            {
                var valyutalar = new List<Valyuta>
    {
        new Valyuta { Kod = "AZN", Ad = "Azerbaycan Manati", Silinib = true },
        new Valyuta { Kod = "USD", Ad = "US Dollar", Silinib = true },
        new Valyuta { Kod = "EUR", Ad = "Euro", Silinib = true }
    };

                context.AddRange(valyutalar);
                await context.SaveChangesAsync();
            }
        }
    }
}
