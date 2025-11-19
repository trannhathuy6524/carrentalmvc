using System.ComponentModel.DataAnnotations;

namespace carrentalmvc.Models
{
    /// <summary>
    /// Yêu cầu trở thành tài xế
    /// User gửi request → CarOwner duyệt
    /// Lương tài xế cố định: 500,000 VNĐ/ngày (không cần thương lượng)
    /// </summary>
    public class DriverRequest
    {
        [Key]
        public int DriverRequestId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string CarOwnerId { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string DriverLicense { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string NationalId { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Experience { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DriverRequestStatus Status { get; set; } = DriverRequestStatus.Pending;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }

        public string? ProcessedBy { get; set; }

        public string? RejectionReason { get; set; }

        // Navigation
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ApplicationUser CarOwner { get; set; } = null!;
        public virtual ApplicationUser? Processor { get; set; }
    }

    public enum DriverRequestStatus
    {
        Pending = 0,      // Chờ duyệt
        Approved = 1,     // Đã duyệt
        Rejected = 2      // Từ chối
    }
}