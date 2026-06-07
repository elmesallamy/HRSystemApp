using HRSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System; // أضف هذا لـ DateTime

namespace HRSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(user, password, false, false);
                if (result.Succeeded)
                {
                    // ✅ تحسين: توجيه حسب الدور
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                    {
                        return RedirectToAction("Dashboard", "Home");
                    }
                    return RedirectToAction("Index", "Home");
                }
            }
            ViewBag.Error = "البريد الإلكتروني أو كلمة المرور غير صحيحة";
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string email, string password, string name, string position)
        {
            // إنشاء مستخدم جديد
            var user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                // إضافة المستخدم لدور Employee
                await _userManager.AddToRoleAsync(user, "Employee");

                // إنشاء موظف في جدول Employees
                var employee = new Employee
                {
                    Name = name,
                    Email = email,
                    Position = position,
                    Department = "عام",
                    HireDate = DateTime.Today,
                    Salary = 0,
                    IsActive = true,
                    Phone = "غير مدخل"
                };
                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                // تسجيل الدخول تلقائياً بعد التسجيل
                await _signInManager.SignInAsync(user, isPersistent: false);

                // ✅ تحسين: استخدام TempData بدلاً من ViewBag
                TempData["Success"] = "✅ تم إنشاء حسابك بنجاح!";

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "حدث خطأ: " + string.Join(", ", result.Errors.Select(e => e.Description));
            return View();
        }
    }
}