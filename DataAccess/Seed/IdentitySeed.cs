using FinNex.DataAccess.Contexts;
using FinNex.Domain;
using FinNex.Domain.Entities;
using FinNex.Domain.Entities.HR;
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
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            string[] roles = { RoleNames.Admin, RoleNames.Operator, RoleNames.Viewer };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new AppRole { Name = role });
                }
            }

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

                if (!result.Succeeded)
                    return;
            }

            if (!await userManager.IsInRoleAsync(adminUser, RoleNames.Admin))
            {
                await userManager.AddToRoleAsync(adminUser, RoleNames.Admin);
            }

            // Valyutalar
            if (!await context.Set<Valyuta>().AnyAsync())
            {
                var valyutalar = new List<Valyuta>
                {
                    new Valyuta { Kod = "AZN", Ad = "AZN", Silinib = true },
                    new Valyuta { Kod = "USD", Ad = "USD", Silinib = true },
                    new Valyuta { Kod = "EUR", Ad = "EUR", Silinib = true }
                };

                context.AddRange(valyutalar);
                await context.SaveChangesAsync();
            }

            // Admin üçün bütün permission-lar
            var allPermissions = await context.Permissions.ToListAsync();

            foreach (var permission in allPermissions)
            {
                bool exists = await context.UserPermissions
                    .AnyAsync(x => x.UserId == adminUser.Id && x.PermissionId == permission.Id);

                if (!exists)
                {
                    context.UserPermissions.Add(new UserPermission
                    {
                        UserId = adminUser.Id,
                        PermissionId = permission.Id,
                        Allowed = true
                    });
                }
            }

            await context.SaveChangesAsync();
        }
    }
}