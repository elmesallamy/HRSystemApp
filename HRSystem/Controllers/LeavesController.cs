using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRSystem.Models;
using System.Security.Claims;

namespace HRSystem.Controllers
{
    [Authorize]
    public class LeavesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LeavesController(ApplicationDbContext context)
        {
            _context = context;
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
            var userEmail = User.Identity.Name;
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == userEmail);

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
            var userEmail = User.Identity.Name;
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == userEmail);

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
            var userEmail = User.Identity.Name;
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == userEmail);

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
                return RedirectToAction(nameof(MyLeaves));
            }

            return View(leave);
        }

        // موافقة على الإجازة (للمدير فقط)
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var leave = await _context.Leaves.FindAsync(id);
            if (leave != null)
            {
                leave.Status = "موافقة";
                leave.ApprovedBy = User.Identity.Name;
                leave.ApprovalDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // رفض الإجازة (للمدير فقط)
        [HttpPost]
        public async Task<IActionResult> Reject(int id, string rejectionReason)
        {
            var leave = await _context.Leaves.FindAsync(id);
            if (leave != null)
            {
                leave.Status = "مرفوضة";
                leave.RejectionReason = rejectionReason;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // أضف هذه الدالة الجديدة للرفض بطريقة GET (بديل)
        [HttpGet]
        public async Task<IActionResult> RejectGet(int id, string rejectionReason)
        {
            var leave = await _context.Leaves.FindAsync(id);
            if (leave != null)
            {
                leave.Status = "مرفوضة";
                leave.RejectionReason = rejectionReason ?? "بدون سبب";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}