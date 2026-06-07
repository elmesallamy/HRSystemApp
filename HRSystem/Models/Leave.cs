using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRSystem.Models
{
    public class Leave
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required]
        [StringLength(50)]
        public string LeaveType { get; set; } = "سنوية"; // سنوية، مرضية، طارئة، بدون راتب

        [StringLength(500)]
        public string? Reason { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "قيد الانتظار"; // قيد الانتظار، موافقة، مرفوضة

        public DateTime RequestDate { get; set; } = DateTime.Now;

        public string? ApprovedBy { get; set; }

        public DateTime? ApprovalDate { get; set; }

        public string? RejectionReason { get; set; }

        // عدد أيام الإجازة (تحسب تلقائياً)
        [NotMapped]
        public int TotalDays
        {
            get
            {
                return (EndDate - StartDate).Days + 1;
            }
        }
    }
}