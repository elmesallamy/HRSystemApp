using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HRSystem.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HRSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SalariesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalariesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // عرض الرواتب مع فلتر حسب الشهر والسنة
        public async Task<IActionResult> Index(int? month, int? year)
        {
            var currentMonth = month ?? DateTime.Now.Month;
            var currentYear = year ?? DateTime.Now.Year;

            ViewBag.Month = currentMonth;
            ViewBag.Year = currentYear;
            ViewBag.Months = GetMonthsList();
            ViewBag.Years = GetYearsList();

            var salaries = await _context.Salaries
       .Include(s => s.Employee)
       .Where(s => s.Month == currentMonth && s.Year == currentYear)
       .ToListAsync();

            // ✅ الترتيب بعد جلب البيانات (في الذاكرة، مش في قاعدة البيانات)
            salaries = salaries
                .OrderBy(s => s.Employee?.Name ?? "")
                .ToList();

            if (!salaries.Any())
            {
                ViewBag.NoData = true;
            }

            return View(salaries);
        }

        // حساب الرواتب لشهر معين
        [HttpGet]
        public async Task<IActionResult> CalculateSalaries(int month, int year)
{
    try
    {
        int targetMonth = Convert.ToInt32(month);
        int targetYear = Convert.ToInt32(year);

        // جلب جميع الموظفين النشطين
        var employees = await _context.Employees
            .Where(e => e.IsActive == true)
            .ToListAsync();

        // حذف الرواتب الموجودة لهذا الشهر (إن وجدت) لإعادة الحساب
        var existingSalaries = await _context.Salaries
            .Where(s => s.Month == targetMonth && s.Year == targetYear)
            .ToListAsync();

        if (existingSalaries.Any())
        {
            _context.Salaries.RemoveRange(existingSalaries);
            await _context.SaveChangesAsync();
        }

        // حساب راتب كل موظف
        foreach (var employee in employees)
        {
            var startDate = new DateTime(targetYear, targetMonth, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var allAttendances = await _context.Attendances
                .Where(a => a.EmployeeId == employee.Id && a.Date >= startDate && a.Date <= endDate)
                .ToListAsync();

            var absentDays = 0;
            var lateDays = 0;

            foreach (var att in allAttendances)
            {
                if (att.IsPresent == false && att.Status != "إجازة")
                {
                    absentDays++;
                }

                if (att.Status == "متأخر")
                {
                    lateDays++;
                }
            }

            var dailyRate = employee.Salary / 30;
            var deductionAmount = (absentDays * dailyRate) + (lateDays * (dailyRate / 2));

            // ✅ تأكد من تعيين جميع القيم (حتى الصفر)
            var salary = new Salary
            {
                EmployeeId = employee.Id,
                Month = targetMonth,
                Year = targetYear,
                BasicSalary = employee.Salary,
                Allowances = 0,           // ✅ صريحاً = 0
                Deductions = deductionAmount,
                NetSalary = employee.Salary - deductionAmount,
                AbsentDays = absentDays,
                LateDays = lateDays,
                IsPaid = false,
                CreatedAt = DateTime.Now
            };

            _context.Salaries.Add(salary);
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"✅ تم حساب الرواتب لشهر {targetMonth}/{targetYear} بنجاح. عدد الموظفين: {employees.Count}";
    }
    catch (Exception ex)
    {
        TempData["Error"] = $"❌ حدث خطأ: {ex.Message}";
    }

    return RedirectToAction("Index", new { month, year });
}

        // تعديل راتب فردي
        [HttpPost]
        public async Task<IActionResult> EditSalary(int id, decimal allowances, decimal deductions, string notes)
        {
            var salary = await _context.Salaries.FindAsync(id);
            if (salary != null)
            {
                salary.Allowances = allowances;
                salary.Deductions = deductions;
                salary.NetSalary = salary.BasicSalary + allowances - deductions;
                salary.Notes = notes;
                salary.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["Success"] = "✅ تم تحديث الراتب بنجاح";
            }
            return RedirectToAction("Index", new { month = salary?.Month, year = salary?.Year });
        }

        // تأكيد صرف الرواتب
        [HttpPost]
        public async Task<IActionResult> ConfirmPayment(int month, int year)
        {
            var salaries = await _context.Salaries
                .Where(s => s.Month == month && s.Year == year)
                .ToListAsync();

            foreach (var salary in salaries)
            {
                salary.IsPaid = true;
                salary.PaidDate = DateTime.Now;
            }
            await _context.SaveChangesAsync();

            TempData["Success"] = $"✅ تم تأكيد صرف رواتب شهر {month}/{year}";
            return RedirectToAction("Index", new { month, year });
        }

        // حذف سجل راتب
        [HttpPost]
        public async Task<IActionResult> DeleteSalary(int id)
        {
            var salary = await _context.Salaries.FindAsync(id);
            if (salary != null)
            {
                var month = salary.Month;
                var year = salary.Year;
                _context.Salaries.Remove(salary);
                await _context.SaveChangesAsync();
                TempData["Success"] = "✅ تم حذف السجل";
                return RedirectToAction("Index", new { month, year });
            }
            return RedirectToAction("Index");
        }

        // دوال مساعدة
        private SelectList GetMonthsList()
        {
            var months = new[]
            {
                new { Value = 1, Text = "يناير" },
                new { Value = 2, Text = "فبراير" },
                new { Value = 3, Text = "مارس" },
                new { Value = 4, Text = "أبريل" },
                new { Value = 5, Text = "مايو" },
                new { Value = 6, Text = "يونيو" },
                new { Value = 7, Text = "يوليو" },
                new { Value = 8, Text = "أغسطس" },
                new { Value = 9, Text = "سبتمبر" },
                new { Value = 10, Text = "أكتوبر" },
                new { Value = 11, Text = "نوفمبر" },
                new { Value = 12, Text = "ديسمبر" }
            };
            return new SelectList(months, "Value", "Text");
        }

        private SelectList GetYearsList()
        {
            var currentYear = DateTime.Now.Year;
            var years = Enumerable.Range(currentYear - 2, 5)
                .Select(y => new { Value = y, Text = y.ToString() });
            return new SelectList(years, "Value", "Text");
        }
    }
}