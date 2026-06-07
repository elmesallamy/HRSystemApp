using System.Collections.Generic;

namespace HRSystem.Models
{
    public class UserRoleViewModel
    {
        public ApplicationUser User { get; set; }
        public List<string> Roles { get; set; }
        public bool IsAdmin { get; set; }
    }
}