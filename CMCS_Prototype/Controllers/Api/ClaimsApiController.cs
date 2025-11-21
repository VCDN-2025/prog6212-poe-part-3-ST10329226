using CMCS_Prototype.Data;
using CMCS_Prototype.Models;
using CMCS_Prototype.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CMCS_Prototype.Controllers.Api
{



    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Coordinator,Manager")]
    public class ClaimsApiController : ControllerBase
    {
        private readonly CMCSDbContext _context;
        private readonly ClaimPolicyService _policyService;
        private readonly ApprovalWorkflowService _workflowService;

        public ClaimsApiController(CMCSDbContext context,
            ClaimPolicyService policyService,
            ApprovalWorkflowService workflowService)
        {
            _context = context;
            _policyService = policyService;
            _workflowService = workflowService;
        }

        // GET: api/claims/pending
        [HttpGet("pending")]
        public async Task<ActionResult<IEnumerable<ClaimDTO>>> GetPendingClaims()
        {
            var claims = await _context.Claims
                .Where(c => c.Status == "Pending" || c.Status == "Policy Review")
                .Include(c => c.Lecturer)
                .Select(c => new ClaimDTO
                {
                    ClaimID = c.ClaimID,
                    ClaimNumber = c.ClaimNumber,
                    LecturerName = c.Lecturer.Name,
                    TotalHours = c.TotalHours,
                    TotalAmount = c.TotalAmount,
                    DateSubmitted = c.DateSubmitted,
                    Status = c.Status
                })
                .ToListAsync();

            return Ok(claims);
        }

        // POST: api/claims/{id}/auto-approve
        [HttpPost("{id}/auto-approve")]
        public async Task<IActionResult> AutoApprove(int id)
        {
            var claim = await _context.Claims
                .Include(c => c.ClaimLineItems)
                .FirstOrDefaultAsync(c => c.ClaimID == id);

            if (claim == null) return NotFound();

            // Automated workflow check
            var workflowResult = await _workflowService.EvaluateClaimAsync(claim);

            if (workflowResult.CanAutoApprove)
            { 
                //
                var approverIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var approverRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(approverIdString) || !int.TryParse(approverIdString, out int approverId))
                {
                    // If the user's ID is missing, we cannot log the approval.
                    return Unauthorized("User identity claim (ID) not found or invalid.");
                }
                // 

                claim.Status = "Coordinator Approved";
                claim.DateVerified = DateTime.Now;

                // Log to approval history
                _context.ApprovalHistory.Add(new ApprovalHistory
                {
                    ClaimID = id,
                    ApproverID = approverId, // Safely parsed ID
                    Role = approverRole ?? "Unknown", // Safely retrieved role
                    Action = "Auto-Approved",
                    Comments = "Approved via automated workflow",
                    ActionDate = DateTime.Now
                });

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Claim auto-approved",
                    workflowResult
                });
            }

            return Ok(new
            {
                success = false,
                message = "Requires manual review",
                reasons = workflowResult.RejectionReasons
            });
        }
    }
}