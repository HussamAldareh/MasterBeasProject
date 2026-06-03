using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MasterBeasProject.Models
{
    public enum NotificationType
    {
        BookingAccepted,
        BookingRejected,
        ReportReady,
        NewMessage,
        NewBooking,
        FinalPriceSubmitted

    }

    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        [Display(Name = "Content")]
        public string Body { get; set; } = string.Empty;

        [Display(Name = "Notification Type")]
        public NotificationType Type { get; set; }

        [Display(Name = "Is Read ")]
        public bool IsRead { get; set; } = false;

        [Display(Name = "Link")]
        [StringLength(300)]
        public string? Link { get; set; }

        [Display(Name = "Notification Date")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;
    }
}
