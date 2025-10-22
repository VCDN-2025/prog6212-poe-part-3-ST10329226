using CMCS_Prototype.Data;
using CMCS_Prototype.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace CMCS_Prototype.Controllers
{
    // This controller handles pages specific to the Academic Manager role
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
                .Where(c => c.Status == "Coordinator Approved")
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
                if (claim.Status != "Coordinator Approved")
                {
                    TempData["ErrorMessage"] = $"Claim {claim.ClaimNumber} cannot be approved. Current status is '{claim.Status}'. It must be 'Coordinator Approved'.";
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