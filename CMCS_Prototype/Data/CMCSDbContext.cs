using CMCS_Prototype.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace CMCS_Prototype.Data
{
    // The Identity framework uses the primary key type (int) defined here for all its tables (including Roles and UserRoles)
    public class CMCSDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        // 1. Application's main constructor (used when running the app)
        public CMCSDbContext(DbContextOptions<CMCSDbContext> options) : base(options)
        {
        }

        // 2. CRITICAL FIX: Protected constructor for Moq/Unit Testing
        protected CMCSDbContext() : base()
        {
        }

        // This DbSet is used for manual logging, not part of Identity
        public DbSet<ApprovalHistory> ApprovalHistory { get; set; } = null!;

        // --- Database Tables (DbSets) ---
        // All DbSets must be 'virtual' for Moq to override them
        public virtual DbSet<Lecturer> Lecturers { get; set; } = null!;
        public virtual DbSet<AcademicManager> AcademicManagers { get; set; } = null!;
        public virtual DbSet<Coordinator> ProgrammeCoordinators { get; set; } = null!;
        public virtual DbSet<Claim> Claims { get; set; } = null!;
        public virtual DbSet<ClaimLineItem> ClaimLineItems { get; set; } = null!;
        public virtual DbSet<SupportingDocument> SupportingDocuments { get; set; } = null!;


        // --- Model Configuration (Fluent API for inheritance/keys) ---
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Ensure Identity tables use the correct int keys
            builder.Entity<IdentityUserRole<int>>().ToTable("AspNetUserRoles");
            builder.Entity<IdentityRole<int>>().ToTable("AspNetRoles");
            // ... (Other Identity configurations if needed)

            builder.Entity<Lecturer>()
                .Property(l => l.ContractorNumber)
                .IsRequired();

            builder.Entity<Claim>()
                .HasMany(c => c.ClaimLineItems)
                .WithOne(li => li.Claim)
                .HasForeignKey(li => li.ClaimId)
                .IsRequired();

            builder.Entity<Claim>()
                .HasMany(c => c.SupportingDocuments)
                .WithOne(d => d.Claim)
                .HasForeignKey(d => d.ClaimID)
                .IsRequired();

        }

        // --- Database Seeding Method ---
        public static void SeedData(CMCSDbContext context)
        {
            // IMPORTANT: If you change the seeding logic, you might need to delete 
            // your current SQLite file and run the application to re-seed the database.

            // Allow re-seeding if the main user doesn't exist to ensure the new password is set
            if (context.Users.Any(u => u.Email == "lecturer@cmcs.edu"))
            {
                // If users exist, we must force a delete/recreate process to apply the new password.
                // Since we can't delete the DB here, we'll proceed with the assumption
                // the existing data is either wrong or needs to be re-hashed.
                // For a reliable fix, you MUST delete your current CMCS_Prototype.db file (or drop the database)
                // and then run the application again to trigger the seeding below.
            }

            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();

            // =========================================================================
            // 1. ROLE CREATION (NEW)
            // =========================================================================
            if (!context.Roles.Any())
            {
                context.Roles.AddRange(

                    new IdentityRole<int> { Id = 1, Name = "Lecturer", NormalizedName = "LECTURER" },
                    new IdentityRole<int> { Id = 2, Name = "Coordinator", NormalizedName = "COORDINATOR" },
                    new IdentityRole<int> { Id = 3, Name = "Manager", NormalizedName = "MANAGER" }, 
                    new IdentityRole<int> { Id = 4, Name = "HR", NormalizedName = "HR" }
                );
                context.SaveChanges();
            }

            // =========================================================================
            // 2. USER CREATION
            // =========================================================================
            // Check again if users exist by a known email to prevent duplicates
            if (!context.Users.Any(u => u.Email == "lecturer@cmcs.edu"))
            {
                // Create Users
                var lecturerUser = new Lecturer
                {
                    Id = 1, // Manually assign IDs for predictable seeding
                    UserName = "lecturer@cmcs.edu",
                    Email = "lecturer@cmcs.edu",
                    EmailConfirmed = true,
                    Name = "L. Thompson",
                    ContractorNumber = "CONT-001",
                    DefaultHourlyRate = 150.00m,
                    NormalizedEmail = "LECTURER@CMCS.EDU",
                    NormalizedUserName = "LECTURER@CMCS.EDU",
                    SecurityStamp = System.Guid.NewGuid().ToString() // Required
                };
                // FIX: Setting password to "password"
                lecturerUser.PasswordHash = hasher.HashPassword(lecturerUser, "password");

                var coordinatorUser = new Coordinator
                {
                    Id = 2,
                    UserName = "coordinator@cmcs.edu",
                    Email = "coordinator@cmcs.edu",
                    EmailConfirmed = true,
                    Name = "C. Smith",
                    NormalizedEmail = "COORDINATOR@CMCS.EDU",
                    NormalizedUserName = "COORDINATOR@CMCS.EDU",
                    SecurityStamp = System.Guid.NewGuid().ToString()
                };
                // FIX: Setting password to "password"
                coordinatorUser.PasswordHash = hasher.HashPassword(coordinatorUser, "password");

                var managerUser = new AcademicManager
                {
                    Id = 3,
                    UserName = "manager@cmcs.edu",
                    Email = "manager@cmcs.edu",
                    EmailConfirmed = true,
                    Name = "M. Jones",
                    NormalizedEmail = "MANAGER@CMCS.EDU",
                    NormalizedUserName = "MANAGER@CMCS.EDU",
                    SecurityStamp = System.Guid.NewGuid().ToString()
                };
                // FIX: Setting password to "password"
                managerUser.PasswordHash = hasher.HashPassword(managerUser, "password");


                var hrUser = new User // Use the base 'User' class if there is no special 'HR' model
                {
                    Id = 4, // Next sequential ID
                    UserName = "hr@cmcs.edu", // Use a unique test email
                    Email = "hr@cmcs.edu",
                    EmailConfirmed = true,
                    Name = "H. Admin",
                    NormalizedEmail = "HR@CMCS.EDU",
                    NormalizedUserName = "HR@CMCS.EDU",
                    SecurityStamp = System.Guid.NewGuid().ToString()
                };
                // FIX: Setting password to "password"
                hrUser.PasswordHash = hasher.HashPassword(hrUser, "password");

                // Update the AddRange call
                context.Users.AddRange(lecturerUser, coordinatorUser, managerUser, hrUser);
                context.SaveChanges(); // Save users to make sure IDs are committed


                // =========================================================================
                // 3. ROLE ASSIGNMENT (NEW - Creates the AspNetUserRoles entries)
                // We use the manually assigned IDs 1, 2, 3 for both users and roles.
                // =========================================================================

                // Lecturer (ID 1) gets Lecturer Role (ID 1)
                context.UserRoles.Add(new IdentityUserRole<int> { UserId = 1, RoleId = 1 });

                // Coordinator (ID 2) gets Coordinator Role (ID 2)
                context.UserRoles.Add(new IdentityUserRole<int> { UserId = 2, RoleId = 2 });

                // Manager (ID 3) gets Manager Role (ID 3)
                context.UserRoles.Add(new IdentityUserRole<int> { UserId = 3, RoleId = 3 });

                context.UserRoles.Add(new IdentityUserRole<int> { UserId = 4, RoleId = 4 }); // Use ID 4 for both

                context.SaveChanges();
            }


            // =========================================================================
            // 4. CLAIM CREATION
            // =========================================================================
            if (!context.Claims.Any())
            {
                // Get Lecturer ID (which is 1)
                var lecturerId = context.Users.OfType<Lecturer>().First(u => u.Email == "lecturer@cmcs.edu").Id;

                // 4. Create Sample Claim for the Lecturer
                var sampleClaim = new Claim
                {
                    LecturerID = lecturerId,
                    Status = "Pending",
                    TotalHours = 5.0m,
                    RatePerHour = 150.00m,
                    TotalAmount = 750.00m,
                    DateSubmitted = System.DateTime.Now,
                    ClaimNumber = "CMCS-0001"
                };

                // 5. Create Sample Line Item for the Claim
                var lineItem = new ClaimLineItem
                {
                    // ClaimID will be set automatically on save
                    DateOfActivity = System.DateTime.Now.AddDays(-5),
                    Hours = 5.0m,
                    RatePerHour = 150.00m,
                    ActivityDescription = "Lecture prep for Module X"
                };
                sampleClaim.ClaimLineItems = new List<ClaimLineItem> { lineItem };

                // 6. Create a Sample Supporting Document Placeholder
                var document = new SupportingDocument
                {
                    // ClaimID will be set automatically on save
                    FileName = "InitialContract_LThompson.pdf",
                    MimeType = "application/pdf",
                    FileContent = new byte[] { 0x01, 0x02, 0x03, 0x04 }
                };
                sampleClaim.SupportingDocuments = new List<SupportingDocument> { document };


                context.Claims.Add(sampleClaim);
                context.SaveChanges();
            }
        }
    }
}