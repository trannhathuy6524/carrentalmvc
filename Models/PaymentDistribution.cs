using carrentalmvc.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace carrentalmvc.Models
{
    /// <summary>
    /// Payment distribution record - tracks how each payment is split between recipients
    /// </summary>
    public class PaymentDistribution
    {
        public int PaymentDistributionId { get; set; }

        /// <summary>
        /// The original payment being distributed
        /// </summary>
        public int PaymentId { get; set; }

        /// <summary>
        /// Who receives this portion (Platform ID, Owner ID, or Driver ID)
        /// </summary>
        public string RecipientId { get; set; } = string.Empty;

        /// <summary>
        /// Type of recipient
        /// </summary>
        public RecipientType RecipientType { get; set; }

        /// <summary>
        /// Amount to be distributed to this recipient
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Distribution status
        /// </summary>
        public PaymentDistributionStatus Status { get; set; } = PaymentDistributionStatus.Pending;

        /// <summary>
        /// When this distribution was processed
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Transaction reference for this distribution (e.g., bank transfer ID)
        /// </summary>
        public string? TransactionReference { get; set; }

        /// <summary>
        /// Additional notes about this distribution
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Error message if distribution failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Payment Payment { get; set; } = null!;
        public virtual ApplicationUser? Recipient { get; set; }
    }
}
