using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMCS_Prototype.Models
{
    /// <summary>
    /// Lecturer inherits from User, giving them basic login credentials and specific properties.
    /// This model manages the Lecturer's relationship with their submitted Claims.
    /// </summary>
    public class Lecturer : User // Assuming inheritance as per UML/best practice
    {
        [Required]
        [MaxLength(20)]
        [Display(Name = "Contractor Number")]
        public string ContractorNumber { get; set; } = string.Empty;


        /// <summary>
        /// Default hourly rate for this lecturer's contract
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Default Hourly Rate")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal DefaultHourlyRate { get; set; } = 150.00m; // ✅ ADD THIS

        /// <summary>
        /// Navigation property for claims submitted by this lecturer
        /// </summary>

        // Non-nullable collection initialized to a new list
        // This addresses the error: Non-nullable property 'Claims' must contain a non-null value

        public ICollection<Claim> Claims { get; set; } = new List<Claim>();
    }
}
