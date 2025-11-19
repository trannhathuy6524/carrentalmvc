namespace carrentalmvc.Models.Enums
{
    /// <summary>
    /// Payment distribution status
    /// </summary>
    public enum PaymentDistributionStatus
    {
        /// <summary>
        /// Pending - Chờ xử lý
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Processing - Đang xử lý
        /// </summary>
        Processing = 1,

        /// <summary>
        /// Completed - Đã hoàn thành
        /// </summary>
        Completed = 2,

        /// <summary>
        /// Failed - Thất bại
        /// </summary>
        Failed = 3,

        /// <summary>
        /// Cancelled - Đã hủy
        /// </summary>
        Cancelled = 4
    }

    /// <summary>
    /// Recipient type for payment distribution
    /// </summary>
    public enum RecipientType
    {
        /// <summary>
        /// Platform - Nền tảng
        /// </summary>
        Platform = 0,

        /// <summary>
        /// Car Owner - Chủ xe
        /// </summary>
        CarOwner = 1,

        /// <summary>
        /// Driver - Tài xế
        /// </summary>
        Driver = 2
    }
}
