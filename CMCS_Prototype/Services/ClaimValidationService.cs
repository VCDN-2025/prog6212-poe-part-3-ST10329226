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
    public class ClaimValidationService
    {
        private readonly ILogger<ClaimValidationService> _logger;
        private readonly CMCSDbContext _context;
        private readonly decimal MAX_HOURS_PER_MONTH = 200m;
        private readonly decimal MAX_HOURS_PER_DAY = 8m;
        private readonly decimal OVERTIME_THRESHOLD = 160m;
        private readonly decimal OVERTIME_MULTIPLIER = 1.5m;

        // ✅ CONSTRUCTOR - Takes 2 arguments (CMCSDbContext and ILogger)
        public ClaimValidationService(CMCSDbContext context, ILogger<ClaimValidationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ValidationResult> ValidateClaimAsync(Claim claim)
        {
            var errors = new List<string>();

            // Null check
            if (claim == null)
            {
                return ValidationResult.Failure("Claim cannot be null");
            }

            // Monthly limit check
            if (claim.TotalHours > MAX_HOURS_PER_MONTH)
            {
                errors.Add($"Monthly hours ({claim.TotalHours:N2}) exceed maximum allowed ({MAX_HOURS_PER_MONTH:N2})");
            }

            // Daily limit check (with null safety)
            if (claim.ClaimLineItems != null)
            {
                var dailyHours = claim.ClaimLineItems
                    .GroupBy(li => li.DateOfActivity.Date)
                    .Select(g => new { Date = g.Key, Hours = g.Sum(li => li.Hours) })
                    .Where(d => d.Hours > MAX_HOURS_PER_DAY)
                    .ToList();

                foreach (var day in dailyHours)
                {
                    errors.Add($"Hours on {day.Date:yyyy-MM-dd} ({day.Hours:N2}) exceed daily limit ({MAX_HOURS_PER_DAY:N2})");
                }
            }

            // Overtime calculation and logging
            if (claim.TotalHours > OVERTIME_THRESHOLD)
            {
                var overtimeHours = claim.TotalHours - OVERTIME_THRESHOLD;

                // Safe rate handling (non-nullable property)
                var rate = claim.RatePerHour > 0 ? claim.RatePerHour : 150.00m;
                var overtimeAmount = overtimeHours * OVERTIME_MULTIPLIER * rate;

                _logger.LogInformation(
                    "Claim {ClaimID} has {OvertimeHours}h overtime, amount: {OvertimeAmount:C}",
                    claim.ClaimID, overtimeHours, overtimeAmount
                );
            }

            // Rate compliance check
            var lecturer = await _context.Lecturers
                .FirstOrDefaultAsync(l => l.Id == claim.LecturerID);

            if (lecturer != null)
            {
                if (claim.RatePerHour != lecturer.DefaultHourlyRate)
                {
                    errors.Add($"Hourly rate ({claim.RatePerHour:C}) does not match contract rate ({lecturer.DefaultHourlyRate:C})");
                }
            }

            return errors.Any()
                ? ValidationResult.Failure(string.Join("; ", errors))
                : ValidationResult.Success();
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public static ValidationResult Success() => new() { IsValid = true };
        public static ValidationResult Failure(string message) => new() { IsValid = false, ErrorMessage = message };
    }
}