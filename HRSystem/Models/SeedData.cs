using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace HRSystem.Models
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // إنشاء الأدوار
            string[] roles = { "Admin", "Employee" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // إنشاء الأدمن الوحيد (لو مش موجود)
            var adminEmail = "admin@hrsystem.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    IsApproved = true,      // ← لازم يكون true
                    FullName = "مدير النظام",
                    RegisteredAt = DateTime.Now,
                    ApprovedAt = DateTime.Now,
                    ApprovedBy = "System"
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");

                    // إنشاء Employee record للأدمن
                    var adminEmployee = new Employee
                    {
                        Name = "مدير النظام",
                        Email = adminEmail,
                        Position = "مدير",
                        Department = "الإدارة",
                        HireDate = DateTime.Today,
                        Salary = 0,
                        IsActive = true,
                        Phone = "غير مدخل"
                    };
                    context.Employees.Add(adminEmployee);
                    await context.SaveChangesAsync();
                }
            }
            else
            {
                // ✅ لو الأدمن موجود لكن مش مفعل، فعله
                if (!adminUser.IsApproved)
                {
                    adminUser.IsApproved = true;
                    adminUser.ApprovedBy = "System";
                    adminUser.ApprovedAt = DateTime.Now;
                    await userManager.UpdateAsync(adminUser);
                }
            }
        }
    }
}