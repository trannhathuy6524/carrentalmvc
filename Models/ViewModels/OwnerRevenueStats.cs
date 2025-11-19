namespace carrentalmvc.Models.ViewModels
{
    /// <summary>
    /// Revenue statistics for Car Owner dashboard
    /// </summary>
    public class OwnerRevenueStats
    {
        /// <summary>
        /// Tổng giá trị đơn hàng
        /// </summary>
        public decimal TotalBookingValue { get; set; }

        /// <summary>
        /// Doanh thu từ cho thuê xe (không bao gồm driver và delivery)
        /// </summary>
        public decimal RentalRevenue { get; set; }

        /// <summary>
        /// Doanh thu từ phí giao xe
        /// </summary>
        public decimal DeliveryRevenue { get; set; }

        /// <summary>
        /// Tổng phí tài xế (KHÔNG thuộc về owner)
        /// </summary>
        public decimal DriverFees { get; set; }

        /// <summary>
        /// Hoa hồng nền tảng đã trả (10%)
        /// </summary>
        public decimal PlatformCommission { get; set; }

        /// <summary>
        /// Doanh thu thực nhận (sau khi trừ hoa hồng và driver)
        /// </summary>
        public decimal NetRevenue { get; set; }

        /// <summary>
        /// Tỷ lệ hoa hồng
        /// </summary>
        public decimal CommissionRate { get; set; }

        /// <summary>
        /// Số đơn đã hoàn thành
        /// </summary>
        public int CompletedRentals { get; set; }

        /// <summary>
        /// Số đơn đang hoạt động
        /// </summary>
        public int ActiveRentals { get; set; }

        /// <summary>
        /// Thanh toán đang chờ
        /// </summary>
        public decimal PendingPayments { get; set; }

        /// <summary>
        /// Thanh toán đã nhận
        /// </summary>
        public decimal CompletedPayments { get; set; }

        /// <summary>
        /// Doanh thu trung bình mỗi đơn
        /// </summary>
        public decimal AverageRevenuePerRental => CompletedRentals > 0 ? NetRevenue / CompletedRentals : 0;

        /// <summary>
        /// % hoa hồng trên tổng booking
        /// </summary>
        public decimal CommissionPercentage => TotalBookingValue > 0 ? (PlatformCommission / TotalBookingValue) * 100 : 0;

        /// <summary>
        /// % driver fee trên tổng booking
        /// </summary>
        public decimal DriverFeePercentage => TotalBookingValue > 0 ? (DriverFees / TotalBookingValue) * 100 : 0;

        /// <summary>
        /// % thu nhập thực của owner
        /// </summary>
        public decimal NetRevenuePercentage => TotalBookingValue > 0 ? (NetRevenue / TotalBookingValue) * 100 : 0;
    }

    /// <summary>
    /// Monthly revenue breakdown
    /// </summary>
    public class MonthlyRevenue
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal TotalBookingValue { get; set; }
        public decimal PlatformCommission { get; set; }
        public decimal DriverFees { get; set; }
        public decimal NetRevenue { get; set; }
        public int RentalCount { get; set; }
    }
}
