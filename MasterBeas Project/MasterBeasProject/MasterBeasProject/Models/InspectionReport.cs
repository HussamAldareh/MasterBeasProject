using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MasterBeasProject.Models
{
    public enum ConditionStatus
    {
        Good,       // سليم
        NeedsWork,  // يحتاج صيانة
        Poor        // حالة سيئة
    }

    public class InspectionReport
    {
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }

        [Display(Name = "Structural Condition")]
        public ConditionStatus StructuralCondition { get; set; }

        [StringLength(500)]
        [Display(Name = "Structural Notes")]
        public string? StructuralNotes { get; set; }

        [Display(Name = "Electrical Condition")]
        public ConditionStatus ElectricalCondition { get; set; }

        [StringLength(500)]
        [Display(Name = "Electrical Notes")]
        public string? ElectricalNotes { get; set; }

        [Display(Name = "Plumbing Condition")]
        public ConditionStatus PlumbingCondition { get; set; }

        [StringLength(500)]
        [Display(Name = "Plumbing Notes")]
        public string? PlumbingNotes { get; set; }

        [Display(Name = "Insulation and Moisture")]
        public ConditionStatus InsulationCondition { get; set; }

        [StringLength(500)]
        [Display(Name = "Insulation Notes")]
        public string? InsulationNotes { get; set; }

        [Display(Name = "Finishing Condition")]
        public ConditionStatus FinishingCondition { get; set; }

        [StringLength(500)]
        [Display(Name = "Finishing Notes")]
        public string? FinishingNotes { get; set; }

        [Required(ErrorMessage = "Overall score is required")]
        [Range(0, 100, ErrorMessage = "Overall score must be between 0 and 100")]
        [Display(Name = "Overall Score / 100")]
        public int OverallScore { get; set; }

        [StringLength(1000)]
        [Display(Name = "Summary")]
        public string? Summary { get; set; }

        [Display(Name = "Report Issued At")]
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Report Number")]
        [StringLength(50)]
        public string ReportNumber { get; set; } = string.Empty;

        // Navigation
        [ForeignKey("BookingId")]
        public Booking Booking { get; set; } = null!;
        public ICollection<ReportImage> Images { get; set; } = new List<ReportImage>();
    }
}
