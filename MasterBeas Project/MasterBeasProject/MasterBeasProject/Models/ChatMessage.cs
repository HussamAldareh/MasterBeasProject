using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MasterBeasProject.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required")]
        [StringLength(1000, MinimumLength = 1, ErrorMessage = "الرسالة بين 1 و 1000 حرف")]
        [Display(Name = "Message")]
        public string Message { get; set; } = string.Empty;

        [Display(Name = "Time Sent")]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Is Read")]
        public bool IsRead { get; set; } = false;

        // Navigation
        [ForeignKey("BookingId")]
        public Booking Booking { get; set; } = null!;

        [ForeignKey("SenderId")]
        public ApplicationUser Sender { get; set; } = null!;
    }
}
