using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMCS_Prototype.Models
{
    public class ClaimLineItem
    {
        [Key]
        public int LineItemID { get; set; }

        // Foreign Key
        [Required]
        public int ClaimID { get; set; }

        // --- Scalar Properties ---

        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Date of Activity")]
        public DateTime DateOfActivity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Hours")]
        public decimal Hours { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Rate Per Hour")]
        public decimal RatePerHour { get; set; }

        [Required]
        [StringLength(255)]
        [Display(Name = "Activity Description")]
        // FIX: Initialize non-nullable string
        public string ActivityDescription { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        // Navigation Property
        [ForeignKey("ClaimID")]
        // FIX: Use null-forgiving operator to satisfy non-nullable warning, 
        // as EF Core initializes this for loaded entities.
        public Claim Claim { get; set; } = null!;
    }
}
