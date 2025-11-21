using CMCS_Prototype.Data;
using CMCS_Prototype.Models;
using Microsoft.AspNetCore.Identity; // Required for User management and Sign-in
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims; // Required for ClaimsPrincipal
using System.Threading.Tasks;

// NOTE: I am assuming your login model is named AppLoginViewModel for clarity
// If your view model is just named LoginViewModel, please adjust the name.
using AppLoginViewModel = CMCS_Prototype.Models.LoginViewModel;

namespace CMCS_Prototype.Controllers
{
    public class HomeController : Controller
    {
        private readonly CMCSDbContext _context;
        // NEW: Services for managing users and handling sign-in
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        // Dependency Injection of all required services
        public HomeController(
            CMCSDbContext context,
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // Home/Index main Login Page
        public IActionResult Index()
        {
            // Pass an empty LoginViewModel to the view for form binding
            // Note: If the user is already authenticated, redirect them to their respective dashboard
            if (User.Identity.IsAuthenticated)
            {
                return RedirectBasedOnRole(User);
            }

            return View(new AppLoginViewModel());
        }

        // Handle POST request for Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AppLoginViewModel model)
        {
            // 1. Basic Model State Validation
            if (!ModelState.IsValid)
            {
                // If validation fails, return the user to the login page with errors
                return View("Index", model);
            }

            // 2. Core ASP.NET Identity Sign-in Attempt
            var result = await _signInManager.PasswordSignInAsync(
                model.Email, // Email is used as the UserName in our seed data
                model.Password,
                isPersistent: false, // Don't persist cookie (no "Remember Me")
                lockoutOnFailure: false); // Don't lock account on failure

            if (result.Succeeded)
            {
                // 3. Authentication Succeeded: Now determine role for redirection
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    return RedirectBasedOnRole(await _signInManager.CreateUserPrincipalAsync(user));
                }
            }

            // 4. Authentication Failed
            ModelState.AddModelError("", "Invalid login attempt.");
            return View("Index", model);
        }

        // New Helper method to handle role-based redirection
        private IActionResult RedirectBasedOnRole(ClaimsPrincipal user)
        {
            if (user.IsInRole("Lecturer"))
            {
                return RedirectToAction("Dashboard", "Lecturer");
            }
            else if (user.IsInRole("Coordinator"))
            {
                return RedirectToAction("PendingClaims", "Coordinator");
            }
            // ?? ADD THIS BLOCK ??
            else if (user.IsInRole("HR"))
            {
                return RedirectToAction("Dashboard", "HR"); // Directs to your newly completed HRController Dashboard
            }
            // ?? END ADDITION ??
            else if (user.IsInRole("Manager"))
            {
                return RedirectToAction("Dashboard", "Manager");
            }

            // Default fallback if role isn't recognized
            return RedirectToAction("Error");
        }
        // Added Logout method to allow users to sign out
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Index), "Home");
        }


        // Existing actions
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