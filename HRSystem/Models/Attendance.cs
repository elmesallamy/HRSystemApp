using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRSystem.Models
{
    public class Attendance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [DataType(DataType.Time)]
        public DateTime? CheckInTime { get; set; }

        [DataType(DataType.Time)]
        public DateTime? CheckOutTime { get; set; }

        [StringLength(50)]
        public string? Status { get; set; }  // حاضر، غائب، متأخر، إجازة

        public bool IsPresent { get; set; } = false;

        public string? Notes { get; set; }
    }
}