using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HRSystem.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HRSystem.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // ✅ أضف هذه الدالة
        public async Task<IActionResult> Dashboard()
        {
            // عدد الموظفين
            var totalEmployees = await _context.Employees.CountAsync();

            // الحضور اليوم
            var today = DateTime.Today;
            var todayAttendance = await _context.Attendances
                .CountAsync(a => a.Date == today && a.IsPresent == true);

            // الإجازات المعلقة
            var pendingLeaves = await _context.Leaves
                .CountAsync(l => l.Status == "قيد الانتظار");

            // عدد الموظفين النشطين
            var activeEmployees = await _context.Employees.CountAsync(e => e.IsActive == true);

            ViewBag.TotalEmployees = totalEmployees;
            ViewBag.TodayAttendance = todayAttendance;
            ViewBag.PendingLeaves = pendingLeaves;
            ViewBag.ActiveEmployees = activeEmployees;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}