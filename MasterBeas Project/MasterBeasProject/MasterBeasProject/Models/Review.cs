using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MasterBeasProject.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }

        [Required]
        public string ClientId { get; set; } = string.Empty;

        [Required]
        public int EngineerProfileId { get; set; }

        [Required(ErrorMessage = "التقييم مطلوب")]
        [Range(1, 5, ErrorMessage = "التقييم بين 1 و 5 نجوم")]
        [Display(Name = "التقييم")]
        public int Rating { get; set; }

        [StringLength(500, ErrorMessage = "التعليق لا يتجاوز 500 حرف")]
        [Display(Name = "التعليق")]
        public string? Comment { get; set; }

        [Display(Name = "تاريخ التقييم")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("BookingId")]
        public Booking Booking { get; set; } = null!;

        [ForeignKey("ClientId")]
        public ApplicationUser Client { get; set; } = null!;

        [ForeignKey("EngineerProfileId")]
        public EngineerProfile EngineerProfile { get; set; } = null!;
    }
}
