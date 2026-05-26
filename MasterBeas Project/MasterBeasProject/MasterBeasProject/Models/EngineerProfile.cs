using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MasterBeasProject.Models
{
    public class EngineerProfile
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Specialization is required")]
        [StringLength(100, ErrorMessage = "Specialization cannot exceed 100 characters")]
        [Display(Name = "Engineering Specialization")]
        public string Specialization { get; set; } = string.Empty;

        [Required(ErrorMessage = "Years of experience is required")]
        [Range(1, 50, ErrorMessage = "Years of experience must be between 1 and 50")]
        [Display(Name = "Years of Experience")]
        public int YearsOfExperience { get; set; }

        [Required(ErrorMessage = "Inspection price is required")]
        [Range(10, 1000, ErrorMessage = "Inspection price must be between 10 and 1000")]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Inspection Price (USD)")]
        public decimal InspectionPrice { get; set; }

        [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters")]
        [Display(Name = "Engineer Bio")]
        public string? Bio { get; set; }

        [Display(Name = "Professional License Number")]
        [StringLength(50)]
        public string? LicenseNumber { get; set; }

        [Display(Name = "Available for Booking")]
        public bool IsAvailable { get; set; } = true;

        [Display(Name = "Average Rating")]
        [Range(0, 5)]
        [Column(TypeName = "decimal(3,2)")]
        public decimal AverageRating { get; set; } = 0;

        [Display(Name = "Total Reviews")]
        public int TotalReviews { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
