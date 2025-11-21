using System.Collections.Generic;

namespace CMCS_Prototype.Models.ViewModels
{
    /// <summary>
    /// ViewModel for HR Dashboard - encapsulates all data needed for the view
    /// </summary>
    public class HRDashboardVM
    {
        /// <summary>
        /// Total number of settled (approved) claims
        /// </summary>
        public int TotalSettledClaims { get; set; }

        /// <summary>
        /// Sum of all pending payment amounts
        /// </summary>
        public decimal TotalPendingPayment { get; set; }

        /// <summary>
        /// List of all lecturers for data management
        /// </summary>
        public List<Lecturer> Lecturers { get; set; } = new List<Lecturer>();
    }
}