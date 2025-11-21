// CMCS_Prototype/Services/ClaimPolicyService.cs

using CMCS_Prototype.Models;
using System.Collections.Generic;
using System.Linq;

namespace CMCS_Prototype.Services
{
    public class ClaimPolicyService
    {
        // Define Policy Rules (Example values)
        private const decimal MaxHoursPerDay = 8.0m;
        private const decimal PolicyHourlyRate = 150.0m; // Example: Check against contract rate

        // Structure to return policy check results
        public class PolicyCheckResult
        {
            public bool IsPolicyCompliant { get; set; } = true;
            public List<string> PolicyViolations { get; set; } = new List<string>();
        }

        public PolicyCheckResult CheckClaimCompliance(Claim claim)
        {
            var result = new PolicyCheckResult();

            // 1. Check Rate Compliance (Ensure the rate used matches the contract rate)
            // Assuming rate is uniform across all line items for simplicity
            if (claim.ClaimLineItems.Any(li => li.RatePerHour != PolicyHourlyRate))
            {
                result.IsPolicyCompliant = false;
                result.PolicyViolations.Add($"Violation: Hourly rate used ({claim.ClaimLineItems.First().RatePerHour:C}) does not match policy rate ({PolicyHourlyRate:C}).");
            }

            // 2. Check Daily Hours Limit
            // Group line items by date to sum hours
            var dailyHours = claim.ClaimLineItems
                .GroupBy(li => li.DateOfActivity.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalHours = g.Sum(li => li.Hours)
                });

            foreach (var day in dailyHours)
            {
                if (day.TotalHours > MaxHoursPerDay)
                {
                    result.IsPolicyCompliant = false;
                    result.PolicyViolations.Add($"Violation: Hours claimed on {day.Date:yyyy-MM-dd} ({day.TotalHours} hours) exceed the daily maximum of {MaxHoursPerDay} hours.");
                }
            }

            return result;
        }
    }
}