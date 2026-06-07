using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace HRSystem.Models
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // إنشاء دور Admin
            string adminRole = "Admin";
            string employeeRole = "Employee";

            if (!await roleManager.RoleExistsAsync(adminRole))
                await roleManager.CreateAsync(new IdentityRole(adminRole));

            if (!await roleManager.RoleExistsAsync(employeeRole))
                await roleManager.CreateAsync(new IdentityRole(employeeRole));

            // إنشاء المستخدم admin
            var adminEmail = "admin@hrsystem.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, adminRole);
                }
            }
        }
    }
}