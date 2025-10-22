using System.ComponentModel.DataAnnotations;
using System;

namespace CMCS_Prototype.Models
{
    public class SupportingDocument
    {
        [Key]
        public int DocumentID { get; set; }
        public int ClaimID { get; set; }    // Foreign Key

        // Data properties
        public string FileName { get; set; } = string.Empty;

        // ADDED/CORRECTED: Required for storing the file's location/URL
        public string FilePath { get; set; } = string.Empty;

        // ADDED/CORRECTED: Required for file type
        public string MimeType { get; set; } = string.Empty;

        // ADDED/CORRECTED: Required for storing small files or for seeding purposes
        public byte[] FileContent { get; set; } = Array.Empty<byte>();

        public DateTime UploadedDate { get; set; }

        // Navigation Property
        public Claim Claim { get; set; } = null!;
    }
}