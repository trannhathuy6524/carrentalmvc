using System.ComponentModel.DataAnnotations;

namespace carrentalmvc.Models
{
    public class Category
    {
        public int CategoryId { get; set; }
        [Required, StringLength(100)]
        public string Name { get; set; }
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<Car> Cars { get; set; } = new List<Car>();
    }

}
