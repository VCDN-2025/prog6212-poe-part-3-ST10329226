namespace CMCS_Prototype.Models.ViewModels
{
    // This is used to strongly type the incoming JSON from the AJAX call
    public class UpdateRateVM
    {
        public int LecturerId { get; set; }
        public decimal Rate { get; set; }
    }
}