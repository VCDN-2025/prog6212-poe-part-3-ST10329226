using CMCS_Prototype.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace CMCS_Prototype.Data
{
    public class CMCSDbContext : DbContext
    {
        // 1. Application's main constructor (used when running the app)
        public CMCSDbContext(DbContextOptions<CMCSDbContext> options) : base(options)
        {
        }

        // 2. CRITICAL FIX: Protected constructor for Moq/Unit Testing
        // This allows the Moq proxying framework (Castle.Core) to instantiate a mock object.
        protected CMCSDbContext() : base()
        {
        }

        // --- Database Tables (DbSets) ---
        // All DbSets must be 'virtual' for Moq to override them
        public virtual DbSet<User> Users { get; set; } = null!;
        public virtual DbSet<Lecturer> Lecturers { get; set; } = null!;
        public virtual DbSet<AcademicManager> AcademicManagers { get; set; } = null!;
        public virtual DbSet<Coordinator> ProgrammeCoordinators { get; set; } = null!;
        public virtual DbSet<Claim> Claims { get; set; } = null!;
        public virtual DbSet<ClaimLineItem> ClaimLineItems { get; set; } = null!;
        public virtual DbSet<SupportingDocument> SupportingDocuments { get; set; } = null!;


        // --- Model Configuration (Fluent API for inheritance/keys) ---
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Lecturer>()
                .Property(l => l.ContractorNumber)
                .IsRequired();

            modelBuilder.Entity<Claim>()
                .HasMany(c => c.ClaimLineItems)
                .WithOne(li => li.Claim)
                .HasForeignKey(li => li.ClaimID)
                .IsRequired();

            modelBuilder.Entity<Claim>()
                .HasMany(c => c.SupportingDocuments)
                .WithOne(d => d.Claim)
                .HasForeignKey(d => d.ClaimID)
                .IsRequired();
        }

        // --- Database Seeding Method ---
        public static void SeedData(CMCSDbContext context)
        {
            if (context.Users.Any())
            {
                return; // DB has been seeded
            }

            const string hashedPasswordPlaceholder = "Pass123";

            // 1. Create Lecturer User
            var lecturerUser = new Lecturer
            {
                Email = "lecturer@cmcs.edu",
                PasswordHash = hashedPasswordPlaceholder,
                UserType = "Lecturer",
                Name = "L. Thompson",
                ContractorNumber = "CONT-001"
            };

            // 2. Create Coordinator User
            var coordinatorUser = new Coordinator
            {
                Email = "coordinator@cmcs.edu",
                PasswordHash = hashedPasswordPlaceholder,
                UserType = "Coordinator",
                Name = "C. Smith"
            };

            // 3. Create Manager User
            var managerUser = new AcademicManager
            {
                Email = "manager@cmcs.edu",
                PasswordHash = hashedPasswordPlaceholder,
                UserType = "Manager",
                Name = "M. Jones"
            };

            context.Users.Add(lecturerUser);
            context.Users.Add(coordinatorUser);
            context.Users.Add(managerUser);

            // Save the users/roles first to get their IDs
            context.SaveChanges();

            // 4. Create Sample Claim for the Lecturer
            var sampleClaim = new Claim
            {
                LecturerID = lecturerUser.UserID,
                Status = "Pending",
                TotalHours = 5.0m,
                RatePerHour = 150.00m,
                TotalAmount = 750.00m,
                DateSubmitted = System.DateTime.Now,
                ClaimNumber = "CMCS-0001"
            };

            context.Claims.Add(sampleClaim);
            context.SaveChanges();

            // 5. Create Sample Line Item for the Claim
            var lineItem = new ClaimLineItem
            {
                ClaimID = sampleClaim.ClaimID,
                DateOfActivity = System.DateTime.Now.AddDays(-5),
                Hours = 5.0m,
                RatePerHour = 150.00m,
                ActivityDescription = "Lecture prep for Module X"
            };

            context.ClaimLineItems.Add(lineItem);

            // 6. Create a Sample Supporting Document Placeholder
            var document = new SupportingDocument
            {
                ClaimID = sampleClaim.ClaimID,
                FileName = "InitialContract_LThompson.pdf",
                MimeType = "application/pdf",
                FileContent = new byte[] { 0x01, 0x02, 0x03, 0x04 }
            };

            context.SupportingDocuments.Add(document);

            context.SaveChanges();
        }
    }
}