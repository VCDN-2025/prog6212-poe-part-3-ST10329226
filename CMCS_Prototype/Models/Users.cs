using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CMCS_Prototype.Models
{
    /// <summary>
    /// Application User model that extends IdentityUser.
    /// IdentityUser provides: Id, Email, PasswordHash, UserName, etc.
    /// Only custom properties unique to our system are added here.
    /// </summary>
    public class User : IdentityUser<int>  // ✅ Inherits all Identity fields
    {
        /// <summary>
        /// Full display name for the user (e.g., "John Smith")
        /// </summary>
        [MaxLength(100)]
        [Display(Name = "Full Name")]
        [PersonalData]  // ✅ Included in GDPR data exports
        public string Name { get; set; } = string.Empty;
    }
}