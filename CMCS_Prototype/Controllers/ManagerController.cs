using CMCS_Prototype.Data;
using CMCS_Prototype.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace CMCS_Prototype.Controllers
{
    // NOTE: Authorization should be added here: [Authorize(Roles = "Manager")]
    public class ManagerController : Controller
    {
        private readonly CMCSDbContext _context;

        public ManagerController(CMCSDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        // GET: /Manager/PendingClaims
        public async Task<IActionResult> PendingClaims()
        {
            var pendingClaims = await _context.Claims
                .Include(c => c.Lecturer)
                // FIX APPLIED: Removed the space to match "CoordinatorApproved" 
                // Assumes the Coordinator sets the status without a space.
                .Where(c => c.Status == "CoordinatorApproved")
                .OrderBy(c => c.DateSubmitted)
                .ToListAsync();

            return View(pendingClaims);
        }

        // /Manager/Approve/5 (FINAL APPROVAL)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var claim = await _context.Claims.FindAsync(id);

            if (claim == null) return NotFound();

            try
            {
                // Implement business rule check for unit test to pass
                // CHECK HERE: Ensure this status string also matches the Coordinator's output
                if (claim.Status != "CoordinatorApproved")
                {
                    TempData["ErrorMessage"] = $"Claim {claim.ClaimNumber} cannot be approved. Current status is '{claim.Status}'. It must be 'CoordinatorApproved'.";
                    return RedirectToAction("PendingClaims");
                }

                // Action is valid - apply final approval
                claim.Status = "Settled"; // Final status
                claim.ManagerID = 3;
                claim.DateVerified = DateTime.Now;

                _context.Update(claim);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Claim {claim.ClaimNumber} has been finally SETTLED.";
                return RedirectToAction("PendingClaims");
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] = "A database error occurred while trying to approve the claim. Please try again.";
                return RedirectToAction("PendingClaims");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An unexpected error occurred during final approval: " + ex.Message;
                return RedirectToAction("PendingClaims");
            }
        }

        // /Manager/Reject/5 (FINAL REJECTION)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var claim = await _context.Claims.FindAsync(id);

            if (claim == null) return NotFound();

            try
            {
                // The rejection logic is sound for unit testing
                claim.Status = "Manager Rejected";
                claim.ManagerID = 3;
                claim.RejectionReason = reason ?? "Claim rejected by academic manager.";

                _context.Update(claim);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Claim {claim.ClaimNumber} has been finally REJECTED.";
                return RedirectToAction("PendingClaims");
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] = "A database error occurred while trying to reject the claim. Please try again.";
                return RedirectToAction("PendingClaims");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An unexpected error occurred during final rejection: " + ex.Message;
                return RedirectToAction("PendingClaims");
            }
        }
    }
}