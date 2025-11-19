using System.ComponentModel.DataAnnotations;

namespace carrentalmvc.Models
{
    /// <summary>
    /// Quan hệ giữa CarOwner và Driver
    /// Mỗi Driver chỉ thuộc về 1 CarOwner
    /// </summary>
    public class DriverAssignment
    {
        [Key]
        public int DriverAssignmentId { get; set; }

        [Required]
        public string CarOwnerId { get; set; } = string.Empty;

        [Required]
        public string DriverId { get; set; } = string.Empty;

        // ✅ THÊM: Lương tài xế đã thỏa thuận
        /// <summary>
        /// Lương tài xế theo ngày (VNĐ) - Đã thỏa thuận trong DriverRequest
        /// </summary>
        [Required]
        [Range(100000, 5000000)]
        public decimal DailyDriverFee { get; set; }

        /// <summary>
        /// Lương tài xế theo giờ (VNĐ) - Tự động tính = DailyDriverFee / 8
        /// </summary>
        public decimal HourlyDriverFee => DailyDriverFee / 8;

        public bool IsActive { get; set; } = true;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RevokedAt { get; set; }

        public string? Notes { get; set; }

        // Navigation Properties
        public virtual ApplicationUser CarOwner { get; set; } = null!;
        public virtual ApplicationUser Driver { get; set; } = null!;
    }
}