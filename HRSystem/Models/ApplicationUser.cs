using Microsoft.AspNetCore.Identity;

namespace HRSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        // هل الحساب مفعل من الأدمن؟
        public bool IsApproved { get; set; } = false;

        // اسم الموظف
        public string FullName { get; set; } = "";

        // تاريخ التسجيل
        public DateTime RegisteredAt { get; set; } = DateTime.Now;

        // الأدمن اللي وافق عليه
        public string? ApprovedBy { get; set; }

        // تاريخ الموافقة
        public DateTime? ApprovedAt { get; set; }
    }
}