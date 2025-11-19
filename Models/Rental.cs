using carrentalmvc.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace carrentalmvc.Models
{
    public class Rental
    {
        public int RentalId { get; set; }
        public int CarId { get; set; }
        public string RenterId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        public RentalStatus Status { get; set; } = RentalStatus.Pending;

        public string? Notes { get; set; }
        public DateTime? PickupDate { get; set; }
        public DateTime? ReturnDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Deposit { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? LateFee { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? DamageFee { get; set; }
        
        // ✅ Driver Assignment Fields
        public string? DriverId { get; set; }
        public bool RequiresDriver { get; set; } = false;
        public DateTime? DriverAssignedAt { get; set; }
        public bool? DriverAccepted { get; set; }
        
        /// <summary>
        /// Phí tài xế ước tính khi khách đặt xe (dựa trên max fee của drivers)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? EstimatedDriverFee { get; set; }
        
        /// <summary>
        /// Phí tài xế thực tế (sau khi driver nhận đơn)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ActualDriverFee { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Car Car { get; set; }
        public virtual ApplicationUser Renter { get; set; }
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ApplicationUser? Driver { get; set; }
    }
}