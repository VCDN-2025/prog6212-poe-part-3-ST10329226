using CMCS_Prototype.Data;
using CMCS_Prototype.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

// NOTE: We need System.IO and System.Security.Claims but will add them only when needed
// The current logic doesn't require DbContext interaction yet, but we'll include it for future use.

namespace CMCS_Prototype.Controllers
{
    public class HomeController : Controller
    {
        private readonly CMCSDbContext _context;

        // Dependency Injection of the database context
        public HomeController(CMCSDbContext context)
        {
            _context = context;
        }

        // Home/Index main Login Page
        public IActionResult Index()
        {
            // Pass an empty LoginViewModel to the view for form binding
            return View(new LoginViewModel());
        }

        // Add 'await Task.CompletedTask
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // 1. Basic Model State Validation (checks [Required] and [EmailAddress] attributes)
            if (!ModelState.IsValid)
            {
                // If validation fails, return the user to the login page with errors
                return View("Index", model);
            }

            // Prototype Authentication Logic HARDCODED for demonstration
   
            // because you haven't included System.Security.Claims or determined the user's role 
            // via the database yet. We must rely only on the hardcoded checks below for the prototype.
            // Add this line to ensure the method is truly asynchronous
            await Task.CompletedTask;

            if (model.Email.ToLower() == "lecturer@test.com" && model.Password == "password")
            {
                // Simulate Lecturer Login
                return RedirectToAction("Dashboard", "Lecturer");
            }
            else if (model.Email.ToLower() == "coordinator@test.com" && model.Password == "password")
            {
                // Simulate Coordinator Login
                // Redirect to the CoordinatorController
                return RedirectToAction("Dashboard", "Coordinator");
            }
            else if (model.Email.ToLower() == "manager@test.com" && model.Password == "password")
            {
                // Simulate Manager Login
                //Redirect to the ManagerController
                return RedirectToAction("Dashboard", "Manager");
            }
            else
            {
                // Authentication failed
                ModelState.AddModelError("", "Invalid login attempt.");
                return View("Index", model);
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
