// Ensure this namespace matches your project's name
namespace CMCS_Prototype.Models
{
    // Inherits all properties from User (UserId, Name, Email, etc.)
    public class Coordinator : User
    {
        // No additional properties are needed here according to the UML/database structure,
        // as the unique ID is handled by the base User class (TPH inheritance).
    }
}