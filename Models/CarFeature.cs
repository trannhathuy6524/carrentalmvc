namespace carrentalmvc.Models
{
    public class CarFeature
    {
        public int CarId { get; set; }
        public int FeatureId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Car Car { get; set; }
        public virtual Feature Feature { get; set; }
    }

}
