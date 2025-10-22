using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http; // Required for IFormFile
using System.Linq; // Required for LINQ extensions like Sum()

namespace CMCS_Prototype.Models
{
    public class Claim
    {
        [Key]
        public int ClaimID { get; set; }

        [Required]
        [StringLength(50)]
        public string ClaimNumber { get; set; } = string.Empty; // Unique identifier for the claim

        // --- User Information ---
        [Required]
        public int LecturerID { get; set; }
        [ForeignKey("LecturerID")]
        public Lecturer Lecturer { get; set; } = null!; // Navigation property to the Lecturer

        // --- Coordinator/Manager Verification Fields ---
        // ADDED TO RESOLVE ERROR: 'Claim' does not contain a definition for 'CoordinatorID'
        public int? CoordinatorID { get; set; } // ID of the coordinator who processed the claim
        [ForeignKey("CoordinatorID")]
        public Coordinator Coordinator { get; set; } = null!; // Navigation property to the Coordinator (needs Coordinator model)

        public int? ManagerID { get; set; } // ID of the manager who approved/rejected the claim
        [ForeignKey("ManagerID")]
        public AcademicManager Manager { get; set; } = null!; // Navigation property to the Manager (needs Manager model)

        public DateTime? DateVerified { get; set; } // Date of approval or rejection
        public string RejectionReason { get; set; } = string.Empty; // Reason for rejection, if applicable

        // --- Claim Status and Dates ---
        [Required]
        public DateTime DateSubmitted { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // e.g., Pending, Approved, Rejected

        // --- Financial Details ---
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalHours { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal RatePerHour { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; }

        // --- Navigation Collections ---
        public ICollection<ClaimLineItem> ClaimLineItems { get; set; } = new List<ClaimLineItem>();

        // ADDED TO RESOLVE ERROR: 'Claim' does not contain a definition for 'SupportingDocuments'
        public ICollection<SupportingDocument> SupportingDocuments { get; set; } = new List<SupportingDocument>();

        // This is used for the IFormFile upload in the controller, NOT mapped to DB
        [NotMapped]
        public IFormFile DocumentFile { get; set; } = null!;

        // --- Business Logic Method (Required by LecturerController) ---
        /// <summary>
        /// Calculates the TotalHours and TotalAmount for the claim based on its line items.
        /// </summary>
        /// <param name="hourlyRate">The rate to apply to the claim.</param>
        public void CalculateTotals(decimal hourlyRate)
        {
            if (ClaimLineItems == null || !ClaimLineItems.Any())
            {
                TotalHours = 0;
                TotalAmount = 0;
                RatePerHour = hourlyRate;
                return;
            }

            // 1. Calculate Total Hours for the claim
            TotalHours = ClaimLineItems.Sum(li => li.Hours);

            // 2. Set the Rate for the claim
            RatePerHour = hourlyRate;

            // 3. Calculate the Total Amount for the claim
            TotalAmount = TotalHours * hourlyRate;

            // 4. Update the individual ClaimLineItem amounts and rates
            
                foreach (var lineItem in ClaimLineItems)
                {
                    lineItem.RatePerHour = hourlyRate;
                    // ✅ CORRECT: Calculate the line item's amount and save it.
                    lineItem.TotalAmount = lineItem.Hours * hourlyRate;
                }
            }
    }
}
