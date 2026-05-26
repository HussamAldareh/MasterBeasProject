using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MasterBeasProject.Models
{
    public enum PropertyType
    {
        Apartment,  // شقة
        Villa,      // فيلا
        Office,     // مكتب
        Shop,       // محل تجاري
        Land        // أرض
    }

    public class PropertyDetails
    {
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }

        [Required(ErrorMessage = "Property type is required")]
        [Display(Name = "Property Type")]
        public PropertyType PropertyType { get; set; }

        [Required(ErrorMessage = "Area is required")]
        [Range(20, 10000, ErrorMessage = "Area must be between 20 and 10000 square meters")]
        [Display(Name = "Area (m²)")]
        public double Area { get; set; }

        [Required(ErrorMessage = "Floor number is required")]
        [Range(0, 100, ErrorMessage = "Floor number must be between 0 and 100")]
        [Display(Name = "Floor Number")]
        public int FloorNumber { get; set; }

        [Range(1, 20, ErrorMessage = "Number of bedrooms must be between 1 and 20")]
        [Display(Name = "Number of Bedrooms")]
        public int? Bedrooms { get; set; }

        [Range(1, 10, ErrorMessage = "Number of bathrooms must be between 1 and 10")]
        [Display(Name = "Number of Bathrooms")]
        public int? Bathrooms { get; set; }

        [Range(1, 200, ErrorMessage = "Building age must be between 1 and 200 years")]
        [Display(Name = "Building Age (Years)")]
        public int? BuildingAge { get; set; }


        [Display(Name = "Has Elevator")]    
        public bool HasElevator { get; set; } = false;

        [Display(Name = "Has Parking")]
        public bool HasParking { get; set; } = false;

        [StringLength(500, ErrorMessage = "Additional description must not exceed 500 characters")]
        [Display(Name = "Additional Description")]
        public string? AdditionalDescription { get; set; }

        // Navigation
        [ForeignKey("BookingId")]
        public Booking Booking { get; set; } = null!;
    }
}
