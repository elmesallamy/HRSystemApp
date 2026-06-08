using HRSystem.Models;
using HRSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _currentUserService = currentUserService;
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
                if (!user.IsApproved)
                {
                    ViewBag.Error = "⏳ حسابك قيد المراجعة. سيتم تفعيله من قبل المدير قريباً.";
                    return View();
                }

                var result = await _signInManager.PasswordSignInAsync(user, password, false, false);
                if (result.Succeeded)
                {
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
        public async Task<IActionResult> Register(string email, string password, string fullName, string position)
        {
            if (string.IsNullOrEmpty(fullName))
            {
                ViewBag.Error = "الاسم كاملاً مطلوب";
                return View();
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                IsApproved = false,
                FullName = fullName,
                RegisteredAt = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Employee");

                var employee = new Employee
                {
                    Name = fullName,
                    Email = email,
                    Position = position ?? "غير محدد",
                    Department = "عام",
                    HireDate = DateTime.Today,
                    Salary = 0,
                    IsActive = false,
                    Phone = "غير مدخل"
                };
                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                TempData["Success"] = "✅ تم تسجيل طلبك بنجاح. سيتم تفعيل حسابك من قبل المدير.";
                return RedirectToAction("Login");
            }

            ViewBag.Error = "حدث خطأ: " + string.Join(", ", result.Errors.Select(e => e.Description));
            return View();
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}