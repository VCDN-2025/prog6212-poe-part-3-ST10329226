using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CMCS_Prototype.Data;
using CMCS_Prototype.Models;

namespace CMCS_Prototype.Services
{
    public class ApprovalWorkflowService
    {
        private readonly CMCSDbContext _context;
        private readonly ILogger<ApprovalWorkflowService> _logger;

        public ApprovalWorkflowService(CMCSDbContext context, ILogger<ApprovalWorkflowService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<WorkflowResult> EvaluateClaimAsync(Claim claim)
        {
            var result = new WorkflowResult { CanAutoApprove = true };

            // ✅ CRITICAL: Ensure Lecturer is loaded before accessing properties
            if (claim == null)
            {
                claim = await _context.Claims
                    .Include(c => c.Lecturer)
                    .Include(c => c.ClaimLineItems)
                    .FirstOrDefaultAsync(c => c.ClaimID == claim.ClaimID);

                if (claim?.Lecturer == null)
                {
                    result.CanAutoApprove = false;
                    result.RejectionReasons.Add("Lecturer information missing");
                    return result;
                }
            }

            var checks = new List<WorkflowCheck>
            {
                new WorkflowCheck
                {
                    Name = "Hours Under Threshold",
                    Passed = claim.TotalHours < 100,
                    Reason = "High hours (>100) require manual review"
                },
                new WorkflowCheck
                {
                    Name = "Rate Approved",
                    Passed = await IsRateApprovedAsync(claim),
                    Reason = "Hourly rate does not match lecturer's contract rate"
                },
                new WorkflowCheck
                {
                    Name = "No Previous Rejections",
                    Passed = await HasNoRejectionsAsync(claim.LecturerID),
                    Reason = "Lecturer has previous claim rejections - manual review needed"
                },
                new WorkflowCheck
                {
                    Name = "Budget Available",
                    Passed = await CheckBudgetAsync(claim),
                    Reason = "Insufficient budget allocated for this module"
                }
            };

            result.Checks = checks;
            result.CanAutoApprove = checks.All(c => c.Passed);
            result.RejectionReasons = checks
                .Where(c => !c.Passed)
                .Select(c => c.Reason)
                .ToList();

            _logger.LogInformation(
                "Claim {ClaimID} evaluation: {Result} - {Reasons}",
                claim.ClaimID,
                result.CanAutoApprove ? "Auto-Approved" : "Manual Review",
                string.Join("; ", result.RejectionReasons)
            );

            return result;
        }

        private async Task<bool> IsRateApprovedAsync(Claim claim)
        {
            try
            {
                // ✅ FIXED: Use claim.LecturerID (int) not claim.Lecturer.Name (string)
                var lecturer = await _context.Lecturers
                    .FirstOrDefaultAsync(l => l.Id == claim.LecturerID);

                if (lecturer == null)
                {
                    _logger.LogWarning("Lecturer {LecturerID} not found for rate check", claim.LecturerID);
                    return false;
                }

                return claim.RatePerHour == lecturer.DefaultHourlyRate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rate check failed for claim {ClaimID}", claim.ClaimID);
                return false;
            }
        }

        private async Task<bool> HasNoRejectionsAsync(int lecturerId)
        {
            try
            {
                var rejectionCount = await _context.Claims
                    .CountAsync(c => c.LecturerID == lecturerId &&
                                   (c.Status == "Rejected" || c.Status == "Manager Rejected"));

                return rejectionCount == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rejection history check failed for lecturer {LecturerID}", lecturerId);
                return false;
            }
        }

        private async Task<bool> CheckBudgetAsync(Claim claim)
        {
            await Task.CompletedTask; // For prototype

            _logger.LogInformation("Budget check passed for claim {ClaimID}", claim.ClaimID);
            return true; // Simplified for prototype
        }

        public async Task LogManualApprovalAsync(int claimId, int approverId, string role, string action, string comments)
        {
            try
            {
                var history = new ApprovalHistory
                {
                    ClaimID = claimId,
                    ApproverID = approverId,
                    Role = role,
                    Action = action,
                    Comments = comments,
                    ActionDate = DateTime.Now
                };

                _context.ApprovalHistory.Add(history);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Approval history logging failed for claim {ClaimID}", claimId);
            }
        }
    }

    public class WorkflowResult
    {
        public bool CanAutoApprove { get; set; }
        public List<string> RejectionReasons { get; set; } = new();
        public List<WorkflowCheck> Checks { get; set; } = new();
    }

    public class WorkflowCheck
    {
        public string Name { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}