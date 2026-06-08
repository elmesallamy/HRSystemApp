using System.Security.Claims;
using HRSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HRSystem.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _context;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        public async Task<Employee?> GetCurrentEmployeeAsync()
        {
            var userEmail = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail))
                return null;

            return await _context.Employees.FirstOrDefaultAsync(e => e.Email == userEmail);
        }
    }
}