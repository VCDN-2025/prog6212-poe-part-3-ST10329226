using CMCS_Prototype.Data;
using CMCS_Prototype.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using CMCS_Prototype.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic; // Added for List<Claim>

namespace CMCS_Prototype.Controllers
{
    // NOTE: Authorization should be added here: [Authorize(Roles = "Coordinator")]
    public class CoordinatorController : Controller
    {
        private readonly CMCSDbContext _context;
        private readonly ClaimPolicyService _policyService;
        private readonly ILogger<CoordinatorController> _logger;

        // CORRECTED: Use proper Dependency Injection
        public CoordinatorController(
            CMCSDbContext context,
            ClaimPolicyService policyService,
            ILogger<CoordinatorController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _policyService = policyService ?? throw new ArgumentNullException(nameof(policyService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IActionResult Dashboard()
        {
            return RedirectToAction("PendingClaims");
        }

        // CORRECTED: Added complete error handling
        public async Task<IActionResult> PendingClaims()
        {
            try
            {
                // FIXED: Check for both common initial statuses to avoid empty results
                var pendingClaims = await _context.Claims
                    .Include(c => c.Lecturer)
                    .Where(c => c.Status == "Pending" || c.Status == "Submitted")
                    .OrderBy(c => c.DateSubmitted)
                    .ToListAsync();

                // FIXED: Return empty list instead of error if no claims found
                if (pendingClaims == null)
                {
                    pendingClaims = new List<Claim>(); // Prevent null reference in view
                }

                return View(pendingClaims);
            }
            catch (Exception ex)
            {
                // FIXED: Log the actual error instead of swallowing it
                _logger.LogError(ex, "Error loading pending claims for coordinator");
                TempData["ErrorMessage"] = $"Error loading claims: {ex.Message}";

                // FIXED: Return empty list to view, not error page
                return View(new List<Claim>());
            }
        }

        // CORRECTED: Added error handling and null checks
        public async Task<IActionResult> ClaimDetails(int id)
        {
            try
            {
                var claim = await _context.Claims
                    .Include(c => c.Lecturer)
                    .Include(c => c.ClaimLineItems)
                    .Include(c => c.SupportingDocuments)
                    .FirstOrDefaultAsync(m => m.ClaimID == id);

                if (claim == null)
                {
                    _logger.LogWarning("Claim {ClaimId} not found", id);
                    return NotFound();
                }

                // FIXED: Added null check before policy check
                var policyResult = _policyService.CheckClaimCompliance(claim);

                ViewBag.PolicyViolations = policyResult.PolicyViolations ?? new List<string>(); // NULL SAFE
                ViewBag.IsPolicyCompliant = policyResult.IsPolicyCompliant;

                return View(claim);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading claim details for claim {ClaimId}", id);
                TempData["ErrorMessage"] = $"Error loading claim details: {ex.Message}";
                return RedirectToAction("PendingClaims");
            }
        }

        // CORRECTED: Improved error handling and validation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var claim = await _context.Claims
                    .Include(c => c.ClaimLineItems)
                    .FirstOrDefaultAsync(c => c.ClaimID == id);

                if (claim == null)
                {
                    return NotFound();
                }

                var policyResult = _policyService.CheckClaimCompliance(claim);

                if (!policyResult.IsPolicyCompliant)
                {
                    claim.Status = "Policy Review";
                    TempData["ErrorMessage"] = "Claim flagged: " + string.Join("; ", policyResult.PolicyViolations);
                }
                else
                {
                    // >>> FIX APPLIED HERE: Removed the space to match ManagerController's filter <<<
                    claim.Status = "CoordinatorApproved";
                    TempData["SuccessMessage"] = $"Claim {claim.ClaimNumber} approved.";
                }

                // FIXED: Use consistent Coordinator ID (should come from current user)
                claim.CoordinatorID = 2; // HARDCODED - replace with User.FindFirst("UserId")?.Value
                claim.DateVerified = DateTime.Now;

                _context.Update(claim);
                await _context.SaveChangesAsync();

                return RedirectToAction("PendingClaims");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error approving claim {ClaimId}", id);
                TempData["ErrorMessage"] = "Another user modified this claim. Please refresh.";
                return RedirectToAction("PendingClaims");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving claim {ClaimId}", id);
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("PendingClaims");
            }
        }

        // CORRECTED: Added validation and logging
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason)) // VALIDATION
            {
                TempData["ErrorMessage"] = "Rejection reason is required.";
                return RedirectToAction("ClaimDetails", new { id });
            }

            try
            {
                var claim = await _context.Claims.FindAsync(id);
                if (claim == null) return NotFound();

                claim.Status = "Rejected";
                claim.CoordinatorID = 2; // HARDCODED
                claim.RejectionReason = reason;
                claim.DateVerified = DateTime.Now;

                _context.Update(claim);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Claim {claim.ClaimNumber} rejected.";
                return RedirectToAction("PendingClaims");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting claim {ClaimId}", id);
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("PendingClaims");
            }
        }
    }
}