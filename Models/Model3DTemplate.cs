using carrentalmvc.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace carrentalmvc.Models
{
    public class Model3DTemplate
    {
        public int TemplateId { get; set; }

        [Required, StringLength(200)]
        public string Name { get; set; }

        public string? Description { get; set; }

        [Required, StringLength(500)]
        public string ModelUrl { get; set; }

        [StringLength(500)]
        public string? PreviewImageUrl { get; set; }

        [Required]
        public FileFormat FileFormat { get; set; }

        public long? FileSize { get; set; }

        public int? BrandId { get; set; }
        public int? CategoryId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Brand? Brand { get; set; }
        public virtual Category? Category { get; set; }
        public virtual ICollection<CarModel3D> CarModels3D { get; set; } = new List<CarModel3D>();
    }
}