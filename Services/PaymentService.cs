using carrentalmvc.Models;
using carrentalmvc.Models.Enums;
using carrentalmvc.Repositories;
using carrentalmvc.Data.Constants;

namespace carrentalmvc.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(IUnitOfWork unitOfWork, ILogger<PaymentService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IEnumerable<Payment>> GetAllPaymentsAsync()
        {
            return await _unitOfWork.Payments.GetAllWithDetailsAsync();
        }

        public async Task<Payment?> GetPaymentByIdAsync(int id)
        {
            return await _unitOfWork.Payments.GetByIdWithDetailsAsync(id);
        }

        public async Task<Payment?> GetPaymentByTransactionIdAsync(string transactionId)
        {
            var payments = await _unitOfWork.Payments.GetAsync(p => p.TransactionId == transactionId);
            return payments.FirstOrDefault();
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByRentalAsync(int rentalId)
        {
            return await _unitOfWork.Payments.GetPaymentsByRentalAsync(rentalId);
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(PaymentStatus status)
        {
            return await _unitOfWork.Payments.GetAsync(p => p.Status == status);
        }

        public async Task<Payment> CreatePaymentAsync(Payment payment)
        {
            try
            {
                payment.CreatedAt = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;

                // ✅ THÊM: Calculate revenue breakdown automatically
                var rental = await _unitOfWork.Rentals.GetByIdAsync(payment.RentalId);
                if (rental != null)
                {
                    var driverFee = rental.ActualDriverFee ?? 0m;
                    var breakdown = PlatformConstants.GetRevenueBreakdown(payment.Amount, driverFee);

                    payment.PlatformFee = breakdown.PlatformFee;
                    payment.OwnerRevenue = breakdown.OwnerRevenue;
                    payment.DriverRevenue = driverFee > 0 ? driverFee : null;
                    payment.CommissionRate = breakdown.CommissionRate;

                    _logger.LogInformation(
                        "Payment breakdown calculated: Total={Total}, Platform={Platform}, Owner={Owner}, Driver={Driver}",
                        payment.Amount, payment.PlatformFee, payment.OwnerRevenue, payment.DriverRevenue ?? 0);
                }

                await _unitOfWork.Payments.AddAsync(payment);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Đã tạo payment mới: ID {PaymentId}", payment.PaymentId);
                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo payment");
                throw;
            }
        }

        public async Task<Payment> UpdatePaymentAsync(Payment payment)
        {
            try
            {
                var existingPayment = await _unitOfWork.Payments.GetByIdAsync(payment.PaymentId);
                if (existingPayment == null)
                {
                    throw new InvalidOperationException($"Không tìm thấy payment với ID: {payment.PaymentId}");
                }

                existingPayment.Amount = payment.Amount;
                existingPayment.PaymentDate = payment.PaymentDate;
                existingPayment.PaymentMethod = payment.PaymentMethod;
                existingPayment.PaymentType = payment.PaymentType;
                existingPayment.Status = payment.Status;
                existingPayment.TransactionId = payment.TransactionId;
                existingPayment.Notes = payment.Notes;
                existingPayment.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Payments.Update(existingPayment);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Đã cập nhật payment ID: {PaymentId}", payment.PaymentId);
                return existingPayment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật payment ID: {PaymentId}", payment.PaymentId);
                throw;
            }
        }

        public async Task<bool> ProcessPaymentAsync(int paymentId, string transactionId)
        {
            try
            {
                var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
                if (payment == null)
                {
                    return false;
                }

                if (payment.Status != PaymentStatus.Pending)
                {
                    throw new InvalidOperationException("Chỉ có thể xử lý thanh toán đang chờ.");
                }

                payment.Status = PaymentStatus.Completed;
                payment.PaymentDate = DateTime.UtcNow;
                payment.TransactionId = transactionId;
                payment.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Payments.Update(payment);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Đã xử lý payment ID: {PaymentId}", paymentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý payment ID: {PaymentId}", paymentId);
                throw;
            }
        }

        public async Task<bool> MarkPaymentCompletedAsync(int paymentId)
        {
            try
            {
                var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
                if (payment == null)
                {
                    return false;
                }

                if (payment.Status != PaymentStatus.Pending)
                {
                    throw new InvalidOperationException("Chỉ có thể đánh dấu hoàn thành cho thanh toán đang chờ.");
                }

                payment.Status = PaymentStatus.Completed;
                payment.PaymentDate = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Payments.Update(payment);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Đã đánh dấu hoàn thành payment ID: {PaymentId}", paymentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đánh dấu hoàn thành payment ID: {PaymentId}", paymentId);
                throw;
            }
        }

        public async Task<bool> MarkPaymentFailedAsync(int paymentId)
        {
            try
            {
                var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
                if (payment == null)
                {
                    return false;
                }

                if (payment.Status != PaymentStatus.Pending)
                {
                    throw new InvalidOperationException("Chỉ có thể đánh dấu thất bại cho thanh toán đang chờ.");
                }

                payment.Status = PaymentStatus.Failed;
                payment.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Payments.Update(payment);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Đã đánh dấu thất bại payment ID: {PaymentId}", paymentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đánh dấu thất bại payment ID: {PaymentId}", paymentId);
                throw;
            }
        }

        public async Task<bool> RefundPaymentAsync(int paymentId, decimal refundAmount, string reason)
        {
            try
            {
                var originalPayment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
                if (originalPayment == null)
                {
                    return false;
                }

                if (originalPayment.Status != PaymentStatus.Completed || originalPayment.Amount <= 0)
                {
                    throw new InvalidOperationException("Chỉ có thể hoàn tiền cho thanh toán đã hoàn thành.");
                }

                if (refundAmount <= 0 || refundAmount > originalPayment.Amount)
                {
                    throw new InvalidOperationException("Số tiền hoàn không hợp lệ.");
                }

                // Tạo payment mới với số tiền âm để thể hiện refund
                var refundPayment = new Payment
                {
                    RentalId = originalPayment.RentalId,
                    Amount = -refundAmount, // Số âm
                    PaymentDate = DateTime.UtcNow,
                    PaymentMethod = originalPayment.PaymentMethod,
                    PaymentType = PaymentType.Refund,
                    Status = PaymentStatus.Completed,
                    TransactionId = $"REFUND_{originalPayment.PaymentId}_{DateTime.UtcNow:yyyyMMddHHmmss}",
                    Notes = reason,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Payments.AddAsync(refundPayment);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Đã tạo refund payment cho ID: {PaymentId}, số tiền: {Amount}",
                    paymentId, refundAmount);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo refund cho payment ID: {PaymentId}", paymentId);
                throw;
            }
        }

        public async Task<bool> CancelPaymentAsync(int paymentId, string reason = "")
        {
            try
            {
                var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
                if (payment == null)
                {
                    return false;
                }

                if (payment.Status != PaymentStatus.Pending)
                {
                    throw new InvalidOperationException("Chỉ có thể hủy thanh toán đang chờ.");
                }

                payment.Status = PaymentStatus.Cancelled;
                payment.Notes = string.IsNullOrEmpty(reason) ? payment.Notes : $"{payment.Notes}. Hủy: {reason}";
                payment.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Payments.Update(payment);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Đã hủy payment ID: {PaymentId}", paymentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hủy payment ID: {PaymentId}", paymentId);
                throw;
            }
        }

        public async Task<Payment> CreateDepositPaymentAsync(int rentalId, decimal amount, PaymentMethod method)
        {
            var payment = new Payment
            {
                RentalId = rentalId,
                Amount = amount,
                PaymentMethod = method,
                PaymentType = PaymentType.Deposit,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return await CreatePaymentAsync(payment);
        }

        public async Task<Payment> CreateRentalPaymentAsync(int rentalId, decimal amount, PaymentMethod method)
        {
            var payment = new Payment
            {
                RentalId = rentalId,
                Amount = amount,
                PaymentMethod = method,
                PaymentType = PaymentType.RentalFee,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return await CreatePaymentAsync(payment);
        }

        public async Task<decimal> GetTotalPaymentsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var payments = await _unitOfWork.Payments.GetAsync(p =>
                p.Status == PaymentStatus.Completed &&
                p.Amount > 0 &&
                (!startDate.HasValue || p.PaymentDate >= startDate.Value) &&
                (!endDate.HasValue || p.PaymentDate < endDate.Value));

            return payments.Sum(p => p.Amount);
        }

        public async Task<decimal> GetTotalPaymentsByOwnerAsync(string ownerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                // Lấy tất cả payments từ rental của owner này
                var payments = await _unitOfWork.Payments.GetAsync(p =>
                    p.Status == PaymentStatus.Completed &&
                    p.Amount > 0 &&
                    p.Rental != null &&
                    p.Rental.Car != null &&
                    p.Rental.Car.OwnerId == ownerId &&
                    (!startDate.HasValue || p.PaymentDate >= startDate.Value) &&
                    (!endDate.HasValue || p.PaymentDate < endDate.Value));

                return payments.Sum(p => p.Amount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy tổng thanh toán cho owner: {OwnerId}", ownerId);
                return 0;
            }
        }

        public async Task<IEnumerable<Payment>> GetPaymentsInDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _unitOfWork.Payments.GetAsync(p =>
                p.PaymentDate >= startDate && p.PaymentDate < endDate);
        }

        public async Task<bool> ValidatePaymentAsync(Payment payment)
        {
            // Kiểm tra rental có tồn tại
            var rental = await _unitOfWork.Rentals.GetByIdAsync(payment.RentalId);
            if (rental == null)
            {
                return false;
            }

            // Kiểm tra số tiền hợp lệ
            if (payment.Amount <= 0 && payment.PaymentType != PaymentType.Refund)
            {
                return false;
            }

            // Có thể thêm các validation khác
            return true;
        }
    }
}