using System.ComponentModel.DataAnnotations;

namespace HospitalClinicManagementSystem.Models;

public class Bill
{
    public int BillID { get; set; }

    [Required]
    public int PatientID { get; set; }

    [Required]
    public int AppointmentID { get; set; }

    [Required, Range(0.01, 99999999)]
    public decimal Amount { get; set; }

    [Required, StringLength(20)]
    [Display(Name = "Payment status")]
    public string PaymentStatus { get; set; } = "Unpaid";

    [StringLength(30)]
    [Display(Name = "Payment method")]
    public string? PaymentMethod { get; set; }

    [Display(Name = "Bill date")]
    public DateTime BillDate { get; set; }
}
