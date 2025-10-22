using CMCS_Prototype.Data;
using CMCS_Prototype.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Required for Include
using System.Linq;
using System.Threading.Tasks;
using System; // Required for DateTime, Guid, Exception
using System.IO; // Required for Path, Directory, FileStream
using Microsoft.AspNetCore.Http; // Required for IFormFile
using Microsoft.AspNetCore.Hosting; // Required for IWebHostEnvironment

namespace CMCS_Prototype.Controllers
{
    // (Roles = "Lecturer")] // for security in a later stage
    public class LecturerController : Controller
    {
        private readonly CMCSDbContext _context;
        // IWebHostEnvironment to correctly access wwwroot path
        private readonly IWebHostEnvironment _hostingEnvironment;

        // database context and the hosting environment
        public LecturerController(CMCSDbContext context, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }

        //  /Lecturer/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            // HARDCODED LecturerID = 1 for prototype 
            int currentLecturerId = 1;

            // Fetch the lecturer's claims, including the Lecturer navigation property (optional, but useful)
            var lecturerClaims = await _context.Claims
        .Where(c => c.LecturerID == currentLecturerId)
        .OrderByDescending(c => c.DateSubmitted)
        .ToListAsync();

            // The view now handles the case where lecturerClaims is empty, preventing NullReferenceException.
            return View(lecturerClaims);
        }

        //Lecturer/ClaimDetails/5
        public async Task<IActionResult> ClaimDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Fetch the specific claim, including related line items and documents
            var claim = await _context.Claims
        .Include(c => c.ClaimLineItems)
        .Include(c => c.SupportingDocuments)
        .Include(c => c.Lecturer)
        .FirstOrDefaultAsync(m => m.ClaimID == id);

            if (claim == null)
            {
                return NotFound();
            }

            // Hardcoded security check
            if (claim.LecturerID != 1)
            {
                return Forbid();
            }

            return View(claim);
        }

        // Lecturer/SubmitClaim (Serves the blank claim form)
        public IActionResult SubmitClaim()
        {
            // Returns the view with an empty Claim object (for form scaffolding)
            return View(new Claim());
        }

        // /Lecturer/SubmitClaim (Handles the form submission and saves to DB)
        [HttpPost]
        [ValidateAntiForgeryToken]
        // CRITICAL: IFormFile is required to handle the file upload from the form
        public async Task<IActionResult> SubmitClaim(Claim claim, IFormFile documentFile)
        {
            // rely on client-side and model binding for line items, but
            // the RatePerHour is not coming from the form, so we must set it.

            // HARDCODED HOURLY RATE for prototype
            const decimal HOURLY_RATE = 150.00M;

            // ModelState.IsValid check later after fixing the form binding.

            try
            {
                // SET METADATA AND CALCULATE TOTALS
                claim.LecturerID = 1; // HARDCODED
                claim.DateSubmitted = DateTime.Now;
                claim.Status = "Pending";

                // Call the calculation method BEFORE saving
                // This populates TotalHours and TotalAmount based on ClaimLineItems
                claim.CalculateTotals(HOURLY_RATE);

                // SAVE THE CLAIM AND LINE ITEMS 
                // EF Core saves ClaimLineItems automatically because they are attached to the Claim
                _context.Claims.Add(claim);
                await _context.SaveChangesAsync(); // Saves Claim and all associated line items

                // Document Upload (Requirement 3) 
                if (documentFile != null && documentFile.Length > 0)
                {
                    //Use _hostingEnvironment.WebRootPath to get the correct server path
                    string uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + documentFile.FileName;
                    string absoluteFilePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Relative path for database storage (e.g., /uploads/filename.pdf)
                    string relativeFilePathForDb = Path.Combine("/uploads", uniqueFileName).Replace('\\', '/');

                    using (var stream = new FileStream(absoluteFilePath, FileMode.Create))
                    {
                        await documentFile.CopyToAsync(stream);
                    }

                    // Save document reference to the database
                    var supportingDocument = new SupportingDocument
                    {
                        ClaimID = claim.ClaimID,
                        FileName = documentFile.FileName,
                        // FIX: Assigning the correct values to the correct properties
                        FilePath = relativeFilePathForDb,
                        MimeType = documentFile.ContentType,
                        // FileContent is empty since we saved the file to disk
                        FileContent = Array.Empty<byte>(),
                        UploadedDate = DateTime.Now
                    };
                    _context.SupportingDocuments.Add(supportingDocument);

                    // Save the document reference
                    await _context.SaveChangesAsync();
                }

                // Redirect user back to the dashboard upon successful submission
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An unexpected error occurred during claim submission: " + ex.Message);
                return View(claim);
            }
        }
    }
}
