using System;
using System.ComponentModel.DataAnnotations;

namespace CMCS_Prototype.Models
{
    public class ApprovalHistory
    {
        [Key]
        public int HistoryID { get; set; }

        public int ClaimID { get; set; }
        public Claim Claim { get; set; } = null!;

        public int ApproverID { get; set; }
        public User Approver { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = string.Empty; // Coordinator, Manager, HR

        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty; // Approved, Rejected, Auto-Approved

        public string Comments { get; set; } = string.Empty;
        public DateTime ActionDate { get; set; } = DateTime.Now;
    }
}