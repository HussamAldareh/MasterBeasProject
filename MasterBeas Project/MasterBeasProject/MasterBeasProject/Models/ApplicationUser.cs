using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MasterBeasProject.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Full Name must be between 3 and 100 characters")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Registration Date")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Profile Image")]
        public string? ProfileImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public EngineerProfile? EngineerProfile { get; set; }
        public ICollection<Booking> ClientBookings { get; set; } = new List<Booking>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
