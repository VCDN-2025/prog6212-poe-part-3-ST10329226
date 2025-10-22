using CMCS_Prototype.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace CMCS_Prototype.Controllers
{
    // This controller handles all actions related to the Coordinator role.
    public class CoordinatorController : Controller
    {
        private readonly CMCSDbContext _context;

        public CoordinatorController(CMCSDbContext context)
        {
            _context = context;
        }

        // Coordinator/Dashboard
        public IActionResult Dashboard()
        {
            // The Dashboard view will eventually redirect to PendingClaims
            return View();
        }

        // GET: /Coordinator/PendingClaims
        public async Task<IActionResult> PendingClaims()
        {
            // Fetch all claims that are currently in the "Pending" status.
            var pendingClaims = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == "Pending")
                .OrderBy(c => c.DateSubmitted)
                .ToListAsync();

            // Note:a separate Coordinator-specific view, use that instead.
            // If the Coordinator view is named PendingClaims.cshtml, use: return View(pendingClaims);
            return View(pendingClaims);
        }

        // Inside CMCS_Prototype.Controllers.CoordinatorController.cs

        // Coordinator/ClaimDetails/5
        public async Task<IActionResult> ClaimDetails(int id)
        {
            // Fetch the claim, including all related entities needed for a detailed report
            var claim = await _context.Claims
                .Include(c => c.Lecturer) // To display lecturer details
                .Include(c => c.ClaimLineItems) // To display the detailed activities/hours
                .Include(c => c.SupportingDocuments) // To display link the uploaded files
                .FirstOrDefaultAsync(m => m.ClaimID == id);

            if (claim == null)
            {
                // error handling for non-existent resource
                return NotFound();
            }

            // calculation method here if TotalHours/TotalAmount are not

            // Return the Claim object to the View (Views/Coordinator/ClaimDetails.cshtml)
            return View(claim);
        }

        // POST: /Coordinator/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var claim = await _context.Claims.FindAsync(id);

            if (claim == null)
            {
                // This fulfills basic error handling
                return NotFound();
            }

            try
            {
                // Coordinator approval must set the status 
                // to the intermediate state that the Manager is looking for.
                // This ensures the two-stage approval process works correctly.
                claim.Status = "Coordinator Approved"; // STATUS FOR MANAGER REVIEW

                claim.CoordinatorID = 2; // Assuming Coordinator ID 2
                claim.DateVerified = DateTime.Now;

                _context.Update(claim);
                await _context.SaveChangesAsync();

                // Add success message (improves user experience)
                TempData["SuccessMessage"] = $"Claim {claim.ClaimNumber} approved for Manager review.";

                return RedirectToAction("PendingClaims");
            }
            //Mandatory Error Handling (catch database exceptions)
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] = "A database error occurred due to conflicting updates. Please review the claim details and try again.";
                return RedirectToAction("PendingClaims");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An unexpected error occurred during approval: {ex.Message}";
                return RedirectToAction("PendingClaims");
            }
        }

        // Coordinator Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var claim = await _context.Claims.FindAsync(id);

            if (claim == null)
            {
                return NotFound();
            }

            try
            {
                // Coordinator rejection is final for the rejection path
                claim.Status = "Rejected";
                claim.CoordinatorID = 2;
                claim.RejectionReason = reason ?? "Claim rejected by coordinator.";

                _context.Update(claim);
                await _context.SaveChangesAsync();

                // Add success message
                TempData["SuccessMessage"] = $"Claim {claim.ClaimNumber} has been rejected.";

                return RedirectToAction("PendingClaims");
            }
            // Error Handling 
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] = "A database error occurred due to conflicting updates. Please review the claim details and try again.";
                return RedirectToAction("PendingClaims");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An unexpected error occurred during rejection: {ex.Message}";
                return RedirectToAction("PendingClaims");
            }
        }
    }
}