using System.ComponentModel.DataAnnotations;

namespace carrentalmvc.Models
{
    public class Feature
    {
        public int FeatureId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Icon { get; set; } // icon hiển thị trên giao diện

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<CarFeature> CarFeatures { get; set; } = new List<CarFeature>();
    }

}
