namespace carrentalmvc.Data.Constants
{
    /// <summary>
    /// Platform revenue configuration constants
    /// </summary>
    public static class PlatformConstants
    {
        /// <summary>
        /// Platform commission rate (10% = 0.10)
        /// Applied to total rental price
        /// </summary>
        public const decimal COMMISSION_RATE = 0.10m;

        /// <summary>
        /// Platform commission rate as percentage for display
        /// </summary>
        public const string COMMISSION_RATE_DISPLAY = "10%";

        /// <summary>
        /// Minimum commission amount (in VND)
        /// </summary>
        public const decimal MINIMUM_COMMISSION = 10000m; // 10,000 VND

        /// <summary>
        /// Calculate platform fee from total amount
        /// </summary>
        /// <param name="totalAmount">Total rental price</param>
        /// <returns>Platform fee amount</returns>
        public static decimal CalculatePlatformFee(decimal totalAmount)
        {
            var fee = totalAmount * COMMISSION_RATE;
            return Math.Max(fee, MINIMUM_COMMISSION);
        }

        /// <summary>
        /// Calculate owner revenue from total amount and driver fee
        /// </summary>
        /// <param name="totalAmount">Total rental price</param>
        /// <param name="driverFee">Driver fee (if any)</param>
        /// <returns>Car owner revenue</returns>
        public static decimal CalculateOwnerRevenue(decimal totalAmount, decimal driverFee = 0)
        {
            var platformFee = CalculatePlatformFee(totalAmount);
            return totalAmount - platformFee - driverFee;
        }

        /// <summary>
        /// Get revenue breakdown for a rental
        /// </summary>
        public static RevenueBreakdown GetRevenueBreakdown(decimal totalAmount, decimal driverFee = 0)
        {
            var platformFee = CalculatePlatformFee(totalAmount);
            var ownerRevenue = totalAmount - platformFee - driverFee;

            return new RevenueBreakdown
            {
                TotalAmount = totalAmount,
                PlatformFee = platformFee,
                OwnerRevenue = ownerRevenue,
                DriverFee = driverFee,
                CommissionRate = COMMISSION_RATE
            };
        }
    }

    /// <summary>
    /// Revenue breakdown model
    /// </summary>
    public class RevenueBreakdown
    {
        public decimal TotalAmount { get; set; }
        public decimal PlatformFee { get; set; }
        public decimal OwnerRevenue { get; set; }
        public decimal DriverFee { get; set; }
        public decimal CommissionRate { get; set; }

        /// <summary>
        /// Validate that breakdown sums to total
        /// </summary>
        public bool IsValid => 
            Math.Round(PlatformFee + OwnerRevenue + DriverFee, 2) == Math.Round(TotalAmount, 2);
    }
}
