using HRSystem.Models;

namespace HRSystem.Services
{
    public interface ICurrentUserService
    {
        Task<Employee> GetCurrentEmployeeAsync();
    }
}