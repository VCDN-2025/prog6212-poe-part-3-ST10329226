using ClosedXML.Excel;
using CMCS_Prototype.Data;
using CMCS_Prototype.Models;
using CMCS_Prototype.Models.ViewModels;
using CMCS_Prototype.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;

namespace CMCS_Prototype.Controllers
{
    [Authorize(Roles = "HR")]
    public class HRController : Controller
    {
        private readonly CMCSDbContext _context;
        private readonly ReportingService _reportingService;
        private readonly ILogger<HRController> _logger;
        private readonly UserManager<User> _userManager;
        public HRController(CMCSDbContext context, ILogger<HRController> logger, ReportingService reportingService, UserManager<User> userManager)
        {
            _context = context;
            _logger = logger;
            _reportingService = reportingService;
            _userManager = userManager;

        }

        public async Task<IActionResult> Dashboard()
        {
            var reportData = new HRDashboardVM
            {
                TotalSettledClaims = await _context.Claims.CountAsync(c => c.Status == "Settled"),
                TotalPendingPayment = await _context.Claims
                    .Where(c => c.Status == "Settled")
                    .SumAsync(c => c.TotalAmount),
                Lecturers = await _context.Lecturers.ToListAsync()
            };
            return View(reportData);
        }

        // In HRController.cs

        [HttpPost]
        [Route("/api/hr/reset-password")]
        public async Task<IActionResult> ResetLecturerPassword([FromBody] int lecturerId)
        {
            // 1. Fetch the Lecturer object using its ID. 
            // Since Lecturer : User, this object is a User object as well.
            var lecturer = await _context.Lecturers
                .FirstOrDefaultAsync(l => l.Id == lecturerId); // <-- NO .Include() needed

            if (lecturer == null)
            {
                return NotFound(new { message = "Lecturer not found." });
            }

            // 2. Generate a new password reset token using the Lecturer object (which is type User)
            var token = await _userManager.GeneratePasswordResetTokenAsync(lecturer); // <-- Pass the Lecturer object directly

            // 3. Define a new, temporary password
            const string newPassword = "TempPass1!";

            // 4. Reset the password
            var result = await _userManager.ResetPasswordAsync(lecturer, token, newPassword); // <-- Pass the Lecturer object directly

            if (result.Succeeded)
            {
                _logger.LogInformation("Password reset successful for Lecturer ID: {Id}. New temporary password set.", lecturerId);
                return Ok(new { success = true, newTempPassword = newPassword });
            }

            // ... Error handling remains the same ...
            _logger.LogError("Password reset failed for Lecturer ID: {Id}. Errors: {Errors}", lecturerId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest(new { success = false, message = "Failed to reset password." });
        }
        // ... Existing using statements, constructor, and actions ...

        // This is the new endpoint that the AJAX call in Dashboard.cshtml hits.
        [HttpPost]
        [Route("/api/hr/update-rate")] // Defines the route as '/api/hr/update-rate'
        public async Task<IActionResult> UpdateHourlyRate([FromBody] UpdateRateVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 1. Use Entity Framework to find the Lecturer
            var lecturer = await _context.Lecturers.FindAsync(model.LecturerId);

            if (lecturer == null)
            {
                _logger.LogWarning("Attempted to update rate for non-existent lecturer ID: {Id}", model.LecturerId);
                return NotFound(new { message = $"Lecturer with ID {model.LecturerId} not found." });
            }

            // 2. Perform the update
            lecturer.DefaultHourlyRate = model.Rate;

            // 3. Persist changes using Entity Framework
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "Lecturer ID {Id} updated hourly rate to {Rate}",
                    lecturer.Id,
                    model.Rate
                );
                return Ok(new { success = true, message = "Hourly rate updated successfully." });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating rate for Lecturer ID: {Id}", lecturer.Id);
                return StatusCode(500, new { message = "A database error occurred during the update." });
            }
        }


        [HttpPost]
        public async Task<IActionResult> SendPaymentReminders()
        {
            try
            {
                var pendingClaims = await _context.Claims
                    .Include(c => c.Lecturer)
                    .Where(c => c.Status == "Settled" && !c.PaymentProcessed)
                    .ToListAsync();

                foreach (var claim in pendingClaims)
                {
                    // Log reminder instead of sending email (prototype)
                    _logger.LogInformation(
                        "Payment Reminder: Lecturer={Email}, Claim={Claim}, Amount={Amount}",
                        claim.Lecturer.Email, claim.ClaimNumber, claim.TotalAmount
                    );
                }

                TempData["SuccessMessage"] = $"Processed {pendingClaims.Count} payment reminders (logged)";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending payment reminders");
                TempData["ErrorMessage"] = "Error processing reminders";
                return RedirectToAction("Dashboard");
            }
        }

        public async Task<IActionResult> GenerateMonthlyInvoice(int month, int year)
        {
            var claims = await _context.Claims
                .Where(c => c.DateSubmitted.Month == month && c.Status == "Settled")
                .Include(c => c.Lecturer)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Monthly Invoice");

            // Headers
            worksheet.Cell(1, 1).Value = "Claim #";
            worksheet.Cell(1, 2).Value = "Lecturer";
            worksheet.Cell(1, 3).Value = "Hours";
            worksheet.Cell(1, 4).Value = "Amount";

            // Data
            for (int i = 0; i < claims.Count; i++)
            {
                var claim = claims[i];
                worksheet.Cell(i + 2, 1).Value = claim.ClaimNumber;
                worksheet.Cell(i + 2, 2).Value = claim.Lecturer.Name;
                worksheet.Cell(i + 2, 3).Value = (double)claim.TotalHours;
                worksheet.Cell(i + 2, 4).Value = (double)claim.TotalAmount;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Invoice_{year}_{month}.xlsx"
            );
        }
    }
}