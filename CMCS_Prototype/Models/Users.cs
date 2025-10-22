using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Added for Table attribute (optional)

namespace CMCS_Prototype.Models
{
    /// <summary>
    /// The base class for all authenticated users (Lecturer, Coordinator, Manager).
    /// This model is intended for database storage.
    /// </summary>
    // [Table("Users")] // Optional: Use this if your table name differs from the class name
    public class User
    {
        [Key]
        [Required]
        public int UserID { get; private set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Stores the securely hashed and salted password for authentication.
        /// This should NEVER store the plain text password.
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Defines the role of the user (e.g., "Lecturer", "Coordinator", "Manager").
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string UserType { get; set; } = string.Empty;
    }
}
