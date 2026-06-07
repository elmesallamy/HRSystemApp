using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRSystem.Models;
using System.Security.Claims;

namespace HRSystem.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AttendanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // عرض كل سجلات الحضور
        public async Task<IActionResult> Index()
        {
            var attendances = await _context.Attendances
                .Include(a => a.Employee)
                .OrderByDescending(a => a.Date)
                .ToListAsync();
            return View(attendances);
        }

        // تسجيل حضور اليوم
        public async Task<IActionResult> CheckIn()
        {
            var userEmail = User.Identity.Name;
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == userEmail);

            if (employee == null)
            {
                return RedirectToAction("Index", "Employees");
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
                ViewBag.Message = "تم تسجيل الحضور بنجاح";
            }
            else
            {
                ViewBag.Message = "تم تسجيل الحضور مسبقاً اليوم";
            }

            return RedirectToAction("Index");
        }

        // تسجيل انصراف
        public async Task<IActionResult> CheckOut()
        {
            var userEmail = User.Identity.Name;
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == userEmail);

            if (employee == null)
            {
                return RedirectToAction("Index", "Employees");
            }

            var today = DateTime.Today;
            var attendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.Date == today);

            if (attendance != null && attendance.CheckOutTime == null)
            {
                attendance.CheckOutTime = DateTime.Now;
                await _context.SaveChangesAsync();
                ViewBag.Message = "تم تسجيل الانصراف بنجاح";
            }
            else
            {
                ViewBag.Message = "لا يوجد تسجيل حضور اليوم أو تم تسجيل الانصراف مسبقاً";
            }

            return RedirectToAction("Index");
        }
        // تقرير الحضور لموظف معين
        public async Task<IActionResult> Report(int? employeeId, DateTime? fromDate, DateTime? toDate)
        {
            // جلب كل الموظفين للفلتر
            ViewBag.Employees = await _context.Employees.ToListAsync();

            // استعلام الحضور
            var query = _context.Attendances
                .Include(a => a.Employee)
                .AsQueryable();

            // فلترة حسب الموظف
            if (employeeId.HasValue && employeeId.Value > 0)
            {
                query = query.Where(a => a.EmployeeId == employeeId.Value);
                ViewBag.SelectedEmployee = employeeId.Value;
            }

            // فلترة حسب التاريخ
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

            // إحصائيات التقرير
            ViewBag.TotalRecords = attendances.Count;
            ViewBag.PresentCount = attendances.Count(a => a.IsPresent == true);
            ViewBag.AbsentCount = attendances.Count(a => a.IsPresent == false && a.Status != "إجازة");
            ViewBag.LeaveCount = attendances.Count(a => a.Status == "إجازة");

            return View(attendances);
        }

        // تصدير التقرير إلى CSV
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

            // إنشاء ملف CSV
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("التاريخ,الموظف,وقت الحضور,وقت الانصراف,الحالة,ملاحظات");

            foreach (var a in attendances)
            {
                sb.AppendLine($"{a.Date:yyyy-MM-dd},{a.Employee.Name},{a.CheckInTime?.ToString("HH:mm")},{a.CheckOutTime?.ToString("HH:mm")},{a.Status},{a.Notes}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"تقرير_الحضور_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}