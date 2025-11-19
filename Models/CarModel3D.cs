using carrentalmvc.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace carrentalmvc.Models
{
    public class CarModel3D
    {
        public int CarModel3DId { get; set; }

        [Required]
        public int CarId { get; set; }

        public int? TemplateId { get; set; }

        [Required, StringLength(500)]
        public string ModelUrl { get; set; }

        public FileFormat? FileFormat { get; set; }

        public long? FileSize { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Car Car { get; set; }
        public virtual Model3DTemplate? Template { get; set; }
    }
}