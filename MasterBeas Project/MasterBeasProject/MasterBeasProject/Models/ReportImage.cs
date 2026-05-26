using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MasterBeasProject.Models
{
    public class ReportImage
    {
        public int Id { get; set; }

        [Required]
        public int InspectionReportId { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Image URL")]
        public string ImageUrl { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Image Caption")]
        public string? Caption { get; set; }

        [Display(Name = "Upload Date")]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("InspectionReportId")]
        public InspectionReport InspectionReport { get; set; } = null!;
    }
}
