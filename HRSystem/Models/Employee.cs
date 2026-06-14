using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRSystem.Models
{
    public class Employee
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "الاسم مطلوب")]
        [Display(Name = "الاسم كاملاً")]
        public string Name { get; set; }    = string.Empty;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد غير صحيحة")]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "رقم الهاتف")]
        [Phone]
        public string? Phone { get; set; }

        [Display(Name = "المنصب")]
        public string? Position { get; set; }

        [Display(Name = "القسم")]
        public string? Department { get; set; }

        [Display(Name = "تاريخ التعيين")]
        [DataType(DataType.Date)]
        public DateTime HireDate { get; set; } = DateTime.Now;

        [Display(Name = "الراتب الأساسي")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Salary { get; set; }

        [Display(Name = "بدلات السكن والمواصلات")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Allowances { get; set; } = 0;

        [Display(Name = "حوافز شهرية")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Incentives { get; set; } = 0;

        [Display(Name = "حالة الموظف")]
        public bool IsActive { get; set; } = true;

        [NotMapped]
        [Display(Name = "إجمالي الخصومات")]
        public decimal TotalDeductions { get; set; }

        [NotMapped]
        [Display(Name = "ساعات الإضافي")]
        public decimal OvertimeHours { get; set; }

        [NotMapped]
        [Display(Name = "صافي الراتب")]
        public decimal NetSalary => Salary + Allowances + Incentives - TotalDeductions;
    }
}