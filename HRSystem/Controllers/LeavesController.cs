using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRSystem.Models;
using HRSystem.Services; // ✅ أضف هذا الـ using

namespace HRSystem.Controllers
{
    [Authorize]
    public class LeavesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService; // ✅ أضف هذا السطر

        public LeavesController(ApplicationDbContext context, ICurrentUserService currentUserService) // ✅ عدل الكونستركتر
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        // عرض كل طلبات الإجازات (للمدير)
        public async Task<IActionResult> Index()
        {
            var leaves = await _context.Leaves
                .Include(l => l.Employee)
                .OrderByDescending(l => l.RequestDate)
                .ToListAsync();
            return View(leaves);
        }

        // عرض إجازاتي (للموظف العادي)
        public async Task<IActionResult> MyLeaves()
        {
            // ✅ استخدام الخدمة بدلاً من الكود المكرر
            var employee = await _currentUserService.GetCurrentEmployeeAsync();

            if (employee == null)
            {
                return RedirectToAction("Index", "Employees");
            }

            var myLeaves = await _context.Leaves
                .Where(l => l.EmployeeId == employee.Id)
                .OrderByDescending(l => l.RequestDate)
                .ToListAsync();

            return View(myLeaves);
        }

        // طلب إجازة جديد
        public async Task<IActionResult> Create()
        {
            // ✅ استخدام الخدمة
            var employee = await _currentUserService.GetCurrentEmployeeAsync();

            if (employee == null)
            {
                return RedirectToAction("Index", "Employees");
            }

            ViewBag.EmployeeName = employee.Name;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StartDate,EndDate,LeaveType,Reason")] Leave leave)
        {
            // ✅ استخدام الخدمة
            var employee = await _currentUserService.GetCurrentEmployeeAsync();

            if (employee == null)
            {
                return RedirectToAction("Index", "Employees");
            }

            if (ModelState.IsValid)
            {
                leave.EmployeeId = employee.Id;
                leave.Status = "قيد الانتظار";
                leave.RequestDate = DateTime.Now;

                _context.Add(leave);
                await _context.SaveChangesAsync();
                TempData["Success"] = "✅ تم إرسال طلب الإجازة بنجاح";
                return RedirectToAction(nameof(MyLeaves));
            }

            return View(leave);
        }

        // ✅ إضافة صلاحية المدير
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var leave = await _context.Leaves.FindAsync(id);
            if (leave != null)
            {
                leave.Status = "موافقة";
                leave.ApprovedBy = User.Identity.Name;
                leave.ApprovalDate = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["Success"] = "✅ تم قبول طلب الإجازة";
            }
            return RedirectToAction(nameof(Index));
        }

        // ✅ إضافة صلاحية المدير
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int id, string rejectionReason)
        {
            var leave = await _context.Leaves.FindAsync(id);
            if (leave != null)
            {
                leave.Status = "مرفوضة";
                leave.RejectionReason = rejectionReason;
                await _context.SaveChangesAsync();
                TempData["Success"] = "❌ تم رفض طلب الإجازة";
            }
            return RedirectToAction(nameof(Index));
        }

        // ❌ تم حذف دالة RejectGet نهائياً (غير آمنة وغير ضرورية)
    }
}