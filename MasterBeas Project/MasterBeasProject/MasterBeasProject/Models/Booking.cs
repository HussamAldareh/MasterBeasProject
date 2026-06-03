using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MasterBeasProject.Models
{
    public enum BookingStatus
    {
        Pending,      // بانتظار القبول
        Accepted,     // مقبول
        Rejected,     // مرفوض
        InProgress,   // جاري الفحص
        Completed,    // مكتمل
        Cancelled     // ملغي
    }

    public class Booking
    {
        public int Id { get; set; }

        [Required]
        public string ClientId { get; set; } = string.Empty;

        [Required]
        public int EngineerProfileId { get; set; }

        [Required(ErrorMessage = "Property Address is required")]
        [StringLength(300, MinimumLength = 10, ErrorMessage = "Property Address must be between 10 and 300 characters")]
        [Display(Name = "Property Address")]
        public string PropertyAddress { get; set; } = string.Empty;

        [Display(Name = "Latitude")]
        [Column(TypeName = "decimal(10,7)")]
        public decimal? Latitude { get; set; }

        [Display(Name = "Longitude")]
        [Column(TypeName = "decimal(10,7)")]
        public decimal? Longitude { get; set; }

        [Required(ErrorMessage = "Inspection Date is required")]
        [Display(Name = "Inspection Date")]
        [DataType(DataType.DateTime)]
        public DateTime InspectionDate { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        [Display(Name = "Additional Notes")]
        public string? Notes { get; set; }

        [Display(Name = "Booking Status")]
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Paid Price")]
        public decimal Price { get; set; }

        [Display(Name = "Request Date")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Rejection Reason")]
        [StringLength(300)]
        public string? RejectionReason { get; set; }

        // Navigation
        [ForeignKey("ClientId")]
        public ApplicationUser Client { get; set; } = null!;

        [ForeignKey("EngineerProfileId")]
        public EngineerProfile EngineerProfile { get; set; } = null!;

        public PropertyDetails? PropertyDetails { get; set; }
        public InspectionReport? InspectionReport { get; set; }


        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Final Price")]
        public decimal? FinalPrice { get; set; }

        public bool IsPriceApproved { get; set; } = false;
        public Review? Review { get; set; }
        public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    }
}
