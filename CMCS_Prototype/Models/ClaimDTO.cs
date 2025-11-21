namespace CMCS_Prototype.Models
{
    public class ClaimDTO
    {
        public int ClaimID { get; set; }
        public string ClaimNumber { get; set; } = string.Empty;
        public string LecturerName { get; set; } = string.Empty;
        public decimal TotalHours { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime DateSubmitted { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}