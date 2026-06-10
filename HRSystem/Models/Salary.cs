using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRSystem.Models
{
    public class Salary
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        [Required]
        public int Month { get; set; }  // 1-12

        [Required]
        public int Year { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BasicSalary { get; set; }  // الراتب الأساسي

        [Column(TypeName = "decimal(18,2)")]
        public decimal Allowances { get; set; } = 0;  // بدلات

        [Column(TypeName = "decimal(18,2)")]
        public decimal Deductions { get; set; } = 0;  // خصومات

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetSalary { get; set; }  // الصافي

        public int AbsentDays { get; set; } = 0;  // أيام الغياب
        public int LateDays { get; set; } = 0;   // أيام التأخير

        public bool IsPaid { get; set; } = false;  // هل تم الصرف؟
        public DateTime? PaidDate { get; set; }    // تاريخ الصرف

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}