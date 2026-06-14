using HRSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HRSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmployeesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Employees
        public async Task<IActionResult> Index(string searchTerm, string department)
        {
            var query = _context.Employees.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(e => e.Name.Contains(searchTerm) ||
                                        e.Email.Contains(searchTerm) ||
                                        (e.Phone != null && e.Phone.Contains(searchTerm)));
            }

            if (!string.IsNullOrEmpty(department) && department != "الكل")
            {
                query = query.Where(e => e.Department == department);
            }

            var employees = await query.ToListAsync();

            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            foreach (var emp in employees)
            {
                var attendances = await _context.Attendances
                    .Where(a => a.EmployeeId == emp.Id && a.Date.Month == currentMonth && a.Date.Year == currentYear)
                    .ToListAsync();

                var absentDays = attendances.Count(a => a.IsPresent == false && a.Status != "إجازة");
                var lateDays = attendances.Count(a => a.Status == "متأخر");

                var overtimeHours = 0m;
                foreach (var att in attendances)
                {
                    if (att.CheckInTime.HasValue && att.CheckOutTime.HasValue)
                    {
                        var workHours = (att.CheckOutTime.Value - att.CheckInTime.Value).TotalHours;
                        if (workHours > 8)
                        {
                            overtimeHours += (decimal)(workHours - 8);
                        }
                    }
                }
                emp.OvertimeHours = overtimeHours;

                var dailyRate = emp.Salary / 30;
                var absenceDeduction = absentDays * dailyRate;
                var lateDeduction = lateDays * (dailyRate / 2);
                emp.TotalDeductions = absenceDeduction + lateDeduction;
            }

            ViewBag.Departments = await _context.Employees
                .Where(e => e.Department != null)
                .Select(e => e.Department)
                .Distinct()
                .ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedDepartment = department;

            return View(employees);
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees.FirstOrDefaultAsync(m => m.Id == id);
            if (employee == null) return NotFound();

            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var attendances = await _context.Attendances
                .Where(a => a.EmployeeId == id && a.Date.Month == currentMonth && a.Date.Year == currentYear)
                .ToListAsync();

            var absentDays = attendances.Count(a => a.IsPresent == false && a.Status != "إجازة");
            var lateDays = attendances.Count(a => a.Status == "متأخر");

            var overtimeHours = 0m;
            foreach (var att in attendances)
            {
                if (att.CheckInTime.HasValue && att.CheckOutTime.HasValue)
                {
                    var workHours = (att.CheckOutTime.Value - att.CheckInTime.Value).TotalHours;
                    if (workHours > 8) overtimeHours += (decimal)(workHours - 8);
                }
            }

            var dailyRate = employee.Salary / 30;
            employee.TotalDeductions = (absentDays * dailyRate) + (lateDays * (dailyRate / 2));
            employee.OvertimeHours = overtimeHours;

            ViewBag.AbsentDays = absentDays;
            ViewBag.LateDays = lateDays;

            return View(employee);
        }

        // GET: Employees/Create
        public IActionResult Create() => View();

        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Email,Phone,Position,Department,HireDate,Salary,Allowances,Incentives,IsActive")] Employee employee)
        {
            if (ModelState.IsValid)
            {
                _context.Add(employee);
                await _context.SaveChangesAsync();
                TempData["Success"] = "✅ تم إضافة الموظف بنجاح";
                return RedirectToAction(nameof(Index));
            }
            return View(employee);
        }

        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();
            return View(employee);
        }

        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Email,Phone,Position,Department,HireDate,Salary,Allowances,Incentives,IsActive")] Employee employee)
        {
            if (id != employee.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(employee);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "✅ تم تعديل بيانات الموظف بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(employee);
        }

        // GET: Employees/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var employee = await _context.Employees.FirstOrDefaultAsync(m => m.Id == id);
            if (employee == null) return NotFound();
            return View(employee);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null) _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
            TempData["Success"] = "✅ تم حذف الموظف بنجاح";
            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(int id) => _context.Employees.Any(e => e.Id == id);
    }
}