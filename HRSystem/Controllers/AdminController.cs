using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HRSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AdminController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> ManageUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRoles = new List<UserRoleViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles.Add(new UserRoleViewModel
                {
                    User = user,
                    Roles = roles.ToList(),
                    IsAdmin = roles.Contains("Admin")
                });
            }

            return View(userRoles);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && !user.IsApproved)
            {
                user.IsApproved = true;
                user.ApprovedBy = User.Identity.Name;
                user.ApprovedAt = DateTime.Now;

                await _userManager.UpdateAsync(user);

                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == user.Email);
                if (employee != null)
                {
                    employee.IsActive = true;
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = $"✅ تم تفعيل حساب {user.FullName} بنجاح";
            }

            return RedirectToAction("ManageUsers");
        }

        [HttpPost]
        public async Task<IActionResult> ChangeRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, role);

                TempData["Success"] = $"✅ تم تغيير دور {user.FullName} إلى {role}";
            }

            return RedirectToAction("ManageUsers");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && user.Email != "admin@hrsystem.com")
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == user.Email);
                if (employee != null)
                {
                    _context.Employees.Remove(employee);
                }

                await _userManager.DeleteAsync(user);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"✅ تم حذف حساب {user.FullName}";
            }

            return RedirectToAction("ManageUsers");
        }
    }
}