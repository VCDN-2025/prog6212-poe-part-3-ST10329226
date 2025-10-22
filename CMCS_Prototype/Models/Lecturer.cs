using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

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

        // Non-nullable collection initialized to a new list
        // This addresses the error: Non-nullable property 'Claims' must contain a non-null value
        public ICollection<Claim> Claims { get; set; } = new List<Claim>();
    }
}
