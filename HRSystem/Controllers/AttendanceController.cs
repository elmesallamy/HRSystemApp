using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRSystem.Models;
using HRSystem.Services;

namespace HRSystem.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public AttendanceController(ApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        // ✅ عرض سجلات الحضور (تم التعديل لاستخدام الخدمة)
        public async Task<IActionResult> Index()
        {
            var isAdmin = User.IsInRole("Admin");
            List<Attendance> attendances;

            if (isAdmin)
            {
                attendances = await _context.Attendances
                    .Include(a => a.Employee)
                    .OrderByDescending(a => a.Date)
                    .ToListAsync();
            }
            else
            {
                // ✅ استخدام الخدمة بدلاً من User.Identity.Name مباشرة
                var employee = await _currentUserService.GetCurrentEmployeeAsync();

                if (employee == null)
                {
                    TempData["Error"] = "لم يتم العثور على بيانات الموظف";
                    return View(new List<Attendance>());
                }

                attendances = await _context.Attendances
                    .Include(a => a.Employee)
                    .Where(a => a.EmployeeId == employee.Id)
                    .OrderByDescending(a => a.Date)
                    .ToListAsync();
            }

            return View(attendances);
        }

        // تسجيل حضور اليوم
        public async Task<IActionResult> CheckIn()
        {
            var employee = await _currentUserService.GetCurrentEmployeeAsync();

            if (employee == null)
            {
                TempData["Error"] = "لم يتم العثور على بيانات الموظف";
                return RedirectToAction("Index");
            }

            var today = DateTime.Today;
            var existingAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.Date == today);

            if (existingAttendance == null)
            {
                var attendance = new Attendance
                {
                    EmployeeId = employee.Id,
                    Date = today,
                    CheckInTime = DateTime.Now,
                    IsPresent = true,
                    Status = "حاضر"
                };
                _context.Attendances.Add(attendance);
                await _context.SaveChangesAsync();
                TempData["Success"] = "✅ تم تسجيل الحضور بنجاح";
            }
            else
            {
                TempData["Warning"] = "⚠️ تم تسجيل الحضور مسبقاً اليوم";
            }

            return RedirectToAction("Index");
        }

        // تسجيل انصراف
        public async Task<IActionResult> CheckOut()
        {
            var employee = await _currentUserService.GetCurrentEmployeeAsync();

            if (employee == null)
            {
                TempData["Error"] = "لم يتم العثور على بيانات الموظف";
                return RedirectToAction("Index");
            }

            var today = DateTime.Today;
            var attendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.Date == today);

            if (attendance != null && attendance.CheckOutTime == null)
            {
                attendance.CheckOutTime = DateTime.Now;

                if (attendance.CheckInTime.HasValue)
                {
                    var hours = (DateTime.Now - attendance.CheckInTime.Value).TotalHours;
                    attendance.Notes = $"عمل {hours:F1} ساعات";
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "✅ تم تسجيل الانصراف بنجاح";
            }
            else if (attendance == null)
            {
                TempData["Error"] = "❌ لا يوجد تسجيل حضور اليوم. الرجاء تسجيل الحضور أولاً";
            }
            else
            {
                TempData["Warning"] = "⚠️ تم تسجيل الانصراف مسبقاً اليوم";
            }

            return RedirectToAction("Index");
        }

        // تقرير الحضور (للمدير فقط)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Report(int? employeeId, DateTime? fromDate, DateTime? toDate)
        {
            ViewBag.Employees = await _context.Employees.ToListAsync();

            var query = _context.Attendances
                .Include(a => a.Employee)
                .AsQueryable();

            if (employeeId.HasValue && employeeId.Value > 0)
            {
                query = query.Where(a => a.EmployeeId == employeeId.Value);
                ViewBag.SelectedEmployee = employeeId.Value;
            }

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.Date >= fromDate.Value);
                ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            }

            if (toDate.HasValue)
            {
                query = query.Where(a => a.Date <= toDate.Value);
                ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            }

            var attendances = await query
                .OrderByDescending(a => a.Date)
                .ThenBy(a => a.Employee.Name)
                .ToListAsync();

            ViewBag.TotalRecords = attendances.Count;
            ViewBag.PresentCount = attendances.Count(a => a.IsPresent == true);
            ViewBag.AbsentCount = attendances.Count(a => a.IsPresent == false && a.Status != "إجازة");
            ViewBag.LeaveCount = attendances.Count(a => a.Status == "إجازة");

            return View(attendances);
        }

        // تصدير التقرير إلى CSV (للمدير فقط)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportCSV(int? employeeId, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Attendances
                .Include(a => a.Employee)
                .AsQueryable();

            if (employeeId.HasValue && employeeId.Value > 0)
                query = query.Where(a => a.EmployeeId == employeeId.Value);

            if (fromDate.HasValue)
                query = query.Where(a => a.Date >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(a => a.Date <= toDate.Value);

            var attendances = await query
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("التاريخ,الموظف,وقت الحضور,وقت الانصراف,عدد الساعات,الحالة,ملاحظات");

            foreach (var a in attendances)
            {
                var hours = "";
                if (a.CheckInTime.HasValue && a.CheckOutTime.HasValue)
                {
                    var diff = a.CheckOutTime.Value - a.CheckInTime.Value;
                    hours = $"{diff.Hours}:{diff.Minutes:D2}";
                }

                sb.AppendLine($"\"{a.Date:yyyy-MM-dd}\",\"{a.Employee?.Name}\",\"{a.CheckInTime?.ToString("HH:mm")}\",\"{a.CheckOutTime?.ToString("HH:mm")}\",\"{hours}\",\"{a.Status}\",\"{a.Notes}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"تقرير_الحضور_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }
    }
}