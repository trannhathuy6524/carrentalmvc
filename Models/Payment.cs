using carrentalmvc.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace carrentalmvc.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }
        public int RentalId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        // ✅ THÊM: Revenue breakdown fields
        /// <summary>
        /// Hoa hồng nền tảng (Platform commission)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal PlatformFee { get; set; }

        /// <summary>
        /// Doanh thu chủ xe (Car owner revenue)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal OwnerRevenue { get; set; }

        /// <summary>
        /// Doanh thu tài xế (Driver revenue) - nullable vì không phải lúc nào cũng có
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? DriverRevenue { get; set; }

        /// <summary>
        /// Tỷ lệ hoa hồng (Commission rate) - VD: 0.10 = 10%
        /// </summary>
        [Column(TypeName = "decimal(5,4)")]
        public decimal CommissionRate { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public string? TransactionId { get; set; }
        public string? Notes { get; set; }
        public PaymentType PaymentType { get; set; } = PaymentType.Rental;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual Rental Rental { get; set; }
    }
}