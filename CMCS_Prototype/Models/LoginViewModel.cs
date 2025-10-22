using System.ComponentModel.DataAnnotations;

namespace CMCS_Prototype.Models
{
    // This model is specifically for binding the data submitted by the login form.
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
