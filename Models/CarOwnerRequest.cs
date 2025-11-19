using System.ComponentModel.DataAnnotations;

namespace carrentalmvc.Models
{
    /// <summary>
    /// Yêu cầu trở thành chủ xe (Car Owner)
    /// </summary>
    public class CarOwnerRequest
    {
        [Key]
        public int CarOwnerRequestId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số CCCD/CMND")]
        [StringLength(12, MinimumLength = 9, ErrorMessage = "CCCD/CMND phải từ 9-12 số")]
        public string NationalId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        [StringLength(200)]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Số xe dự kiến cho thuê
        /// </summary>
        [Required(ErrorMessage = "Vui lòng nhập số xe dự kiến")]
        [Range(1, 100, ErrorMessage = "Số xe phải từ 1-100")]
        public int ExpectedCarCount { get; set; }

        /// <summary>
        /// Kinh nghiệm cho thuê xe (nếu có)
        /// </summary>
        [StringLength(500)]
        public string? Experience { get; set; }

        /// <summary>
        /// Ghi chú thêm
        /// </summary>
        [StringLength(1000)]
        public string? Notes { get; set; }

        /// <summary>
        /// Trạng thái yêu cầu
        /// </summary>
        [Required]
        public CarOwnerRequestStatus Status { get; set; } = CarOwnerRequestStatus.Pending;

        /// <summary>
        /// Thời gian gửi yêu cầu
        /// </summary>
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Thời gian xử lý
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Người xử lý (Admin)
        /// </summary>
        public string? ProcessedBy { get; set; }

        /// <summary>
        /// Lý do từ chối (nếu bị từ chối)
        /// </summary>
        [StringLength(500)]
        public string? RejectionReason { get; set; }

        // Navigation Properties
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ApplicationUser? Processor { get; set; }
    }

    /// <summary>
    /// Trạng thái yêu cầu làm chủ xe
    /// </summary>
    public enum CarOwnerRequestStatus
    {
        Pending,    // Chờ duyệt
        Approved,   // Đã duyệt
        Rejected    // Từ chối
    }
}
