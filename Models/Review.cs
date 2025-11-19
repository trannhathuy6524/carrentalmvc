using System.ComponentModel.DataAnnotations;

namespace carrentalmvc.Models
{
    public class Review
    {
        public int ReviewId { get; set; }
        public int CarId { get; set; }
        public string UserId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }
        public string? Comment { get; set; }

        public bool IsRecommended { get; set; } = true;
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual Car Car { get; set; }
        public virtual ApplicationUser User { get; set; }
    }

}
