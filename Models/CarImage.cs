using System.ComponentModel.DataAnnotations;

namespace carrentalmvc.Models
{
    public class CarImage
    {
        public int CarImageId { get; set; }

        [Required]
        public int CarId { get; set; }

        [Required, StringLength(500)]
        public string ImageUrl { get; set; }

        [StringLength(200)]
        public string? AltText { get; set; }

        public bool IsPrimary { get; set; } = false; // ảnh chính
        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Car Car { get; set; }
    }

}
