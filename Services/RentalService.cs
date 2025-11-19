using carrentalmvc.Models;
using carrentalmvc.Models.Enums;
using carrentalmvc.Repositories;

namespace carrentalmvc.Services
{
    public class RentalService : IRentalService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RentalService> _logger;

        public RentalService(IUnitOfWork unitOfWork, ILogger<RentalService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IEnumerable<Rental>> GetAllRentalsAsync()
        {
            return await _unitOfWork.Rentals.GetAllWithDetailsAsync();
        }

        public async Task<Rental?> GetRentalByIdAsync(int id)
        {
            return await _unitOfWork.Rentals.GetByIdAsync(id);
        }

        public async Task<Rental?> GetRentalWithDetailsAsync(int id)
        {
            return await _unitOfWork.Rentals.GetRentalWithDetailsAsync(id);
        }

        public async Task<IEnumerable<Rental>> GetRentalsByUserAsync(string userId)
        {
            return await _unitOfWork.Rentals.GetRentalsByUserAsync(userId);
        }

        public async Task<IEnumerable<Rental>> GetRentalsByCarAsync(int carId)
        {
            return await _unitOfWork.Rentals.GetRentalsByCarAsync(carId);
        }

        public async Task<IEnumerable<Rental>> GetRentalsByOwnerAsync(string ownerId)
        {
            return await _unitOfWork.Rentals.GetRentalsByOwnerAsync(ownerId);
        }

        public async Task<IEnumerable<Rental>> GetRentalsByStatusAsync(RentalStatus status)
        {
            return await _unitOfWork.Rentals.GetAsync(r => r.Status == status);
        }

        public async Task<IEnumerable<Rental>> GetActiveRentalsAsync()
        {
            return await _unitOfWork.Rentals.GetAsync(r => r.Status == RentalStatus.Active);
        }

        public async Task<IEnumerable<Rental>> GetOverdueRentalsAsync()
        {
            var today = DateTime.Today;
            return await _unitOfWork.Rentals.GetAsync(r =>
                r.Status == RentalStatus.Active && r.EndDate.Date < today);
        }

        public async Task<Rental> CreateRentalAsync(Rental rental)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                rental.CreatedAt = DateTime.UtcNow;
                rental.UpdatedAt = DateTime.UtcNow;
                rental.Status = RentalStatus.Pending;

                await _unitOfWork.Rentals.AddAsync(rental);
                await _unitOfWork.SaveAsync();

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Đã tạo rental mới: ID {RentalId}", rental.RentalId);
                return rental;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi khi tạo rental");
                throw;
            }
        }

        public async Task<Rental> UpdateRentalAsync(Rental rental)
        {
            try
            {
                var existingRental = await _unitOfWork.Rentals.GetByIdAsync(rental.RentalId);
                if (existingRental == null)
                {
                    throw new InvalidOperationException($"Không tìm thấy rental với ID: {rental.RentalId}");
                }

                existingRental.StartDate = rental.StartDate;
                existingRental.EndDate = rental.EndDate;
                existingRental.TotalPrice = rental.TotalPrice;
                existingRental.Deposit = rental.Deposit;
                existingRental.LateFee = rental.LateFee;
                existingRental.DamageFee = rental.DamageFee;
                existingRental.Status = rental.Status;
                existingRental.Notes = rental.Notes;
                existingRental.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Rentals.Update(existingRental);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Đã cập nhật rental ID: {RentalId}", rental.RentalId);
                return existingRental;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật rental ID: {RentalId}", rental.RentalId);
                throw;
            }
        }

        public async Task<bool> ConfirmRentalAsync(int rentalId)
        {
            var rental = await _unitOfWork.Rentals.GetByIdAsync(rentalId);
            if (rental == null || rental.Status != RentalStatus.Pending)
                return false;

            // ✅ KIỂM TRA THANH TOÁN ĐẶT CỌC với làm tròn
            var payments = await _unitOfWork.Payments.GetPaymentsByRentalAsync(rentalId);
            var totalPaid = payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount);
            var depositRequired = rental.TotalPrice * 0.3m;

            // ✅ FIX: Làm tròn trước khi so sánh để tránh floating point error
            if (Math.Round(totalPaid, 0) < Math.Round(depositRequired, 0))
            {
                throw new InvalidOperationException($"Không thể xác nhận đơn. Yêu cầu đặt cọc {depositRequired:N0} VNĐ, hiện tại chỉ có {totalPaid:N0} VNĐ");
            }

            rental.Status = RentalStatus.Confirmed;
            rental.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Rentals.Update(rental);
            await _unitOfWork.SaveAsync();

            return true;
        }

        public async Task<bool> StartRentalAsync(int rentalId)
        {
            var rental = await _unitOfWork.Rentals.GetRentalWithDetailsAsync(rentalId);
            if (rental == null || rental.Status != RentalStatus.Confirmed)
                return false;

            // ✅ KIỂM TRA THANH TOÁN ĐẶT CỌC với làm tròn
            var payments = await _unitOfWork.Payments.GetPaymentsByRentalAsync(rentalId);
            var totalPaid = payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount);
            var depositRequired = rental.TotalPrice * 0.3m;

            // ✅ FIX: Làm tròn trước khi so sánh
            if (Math.Round(totalPaid, 0) < Math.Round(depositRequired, 0))
            {
                throw new InvalidOperationException($"Không thể bắt đầu đơn thuê. Yêu cầu đặt cọc {depositRequired:N0} VNĐ");
            }

            rental.Status = RentalStatus.Active;
            rental.PickupDate = DateTime.UtcNow;
            rental.UpdatedAt = DateTime.UtcNow;

            if (rental.Car != null)
            {
                rental.Car.Status = CarStatus.Rented;
            }

            _unitOfWork.Rentals.Update(rental);
            await _unitOfWork.SaveAsync();

            return true;
        }

        public async Task<bool> CompleteRentalAsync(int rentalId, decimal? damageFee, string? completionNotes)
        {
            var rental = await _unitOfWork.Rentals.GetRentalWithDetailsAsync(rentalId);
            if (rental == null || rental.Status != RentalStatus.Active)
                throw new InvalidOperationException("Chỉ có thể hoàn thành đơn thuê đang hoạt động");

            // ✅ KIỂM TRA THANH TOÁN ĐẦY ĐỦ
            var payments = await _unitOfWork.Payments.GetPaymentsByRentalAsync(rentalId);
            var totalPaid = payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount);
            var remainingAmount = rental.TotalPrice - totalPaid;

            if (remainingAmount > 0)
            {
                throw new InvalidOperationException($"Không thể hoàn thành đơn thuê. Khách hàng còn nợ {remainingAmount:N0} VNĐ");
            }

            rental.Status = RentalStatus.Completed;
            rental.ReturnDate = DateTime.UtcNow;
            rental.UpdatedAt = DateTime.UtcNow;

            if (damageFee.HasValue && damageFee > 0)
            {
                rental.DamageFee = damageFee.Value;
            }

            if (DateTime.Now > rental.EndDate)
            {
                var lateDays = (DateTime.Now - rental.EndDate).Days;
                rental.LateFee = lateDays * (rental.Car?.PricePerDay ?? 0) * 0.1m;
            }

            if (!string.IsNullOrEmpty(completionNotes))
            {
                rental.Notes += $"\n[Hoàn thành] {completionNotes}";
            }

            if (rental.Car != null)
            {
                rental.Car.Status = CarStatus.Available;
            }

            _unitOfWork.Rentals.Update(rental);
            await _unitOfWork.SaveAsync();

            return true;
        }

        public async Task<bool> CancelRentalAsync(int rentalId, string reason = "")
        {
            try
            {
                var rental = await _unitOfWork.Rentals.GetRentalWithDetailsAsync(rentalId);
                if (rental == null)
                {
                    return false;
                }

                if (rental.Status != RentalStatus.Pending && rental.Status != RentalStatus.Confirmed)
                {
                    throw new InvalidOperationException("Chỉ có thể hủy rental ở trạng thái Pending hoặc Confirmed");
                }

                await _unitOfWork.BeginTransactionAsync();

                rental.Status = RentalStatus.Cancelled;
                rental.Notes = string.IsNullOrEmpty(reason) ? rental.Notes : $"{rental.Notes}\nHủy: {reason}";
                rental.UpdatedAt = DateTime.UtcNow;

                // Nếu xe đang ở trạng thái Rented, chuyển về Available
                if (rental.Car != null && rental.Car.Status == CarStatus.Rented)
                {
                    rental.Car.Status = CarStatus.Available;
                    _unitOfWork.Cars.Update(rental.Car);
                }

                _unitOfWork.Rentals.Update(rental);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Đã hủy rental ID: {RentalId}, lý do: {Reason}", rentalId, reason);
                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi khi hủy rental ID: {RentalId}", rentalId);
                throw;
            }
        }

        public async Task<decimal> CalculateLateFeeAsync(int rentalId)
        {
            var rental = await _unitOfWork.Rentals.GetRentalWithDetailsAsync(rentalId);
            if (rental == null || rental.Car == null)
            {
                return 0;
            }

            var returnDate = rental.ReturnDate ?? DateTime.UtcNow;
            if (returnDate <= rental.EndDate)
            {
                return 0; // Không trễ
            }

            var lateDays = (returnDate.Date - rental.EndDate.Date).Days;
            var dailyLateFee = rental.Car.PricePerDay * 0.1m; // 10% giá thuê mỗi ngày

            return lateDays * dailyLateFee;
        }

        public async Task<bool> IsCarAvailableAsync(int carId, DateTime startDate, DateTime endDate)
        {
            var conflictingRentals = await _unitOfWork.Rentals.GetAsync(r =>
                r.CarId == carId &&
                r.Status != RentalStatus.Cancelled &&
                r.Status != RentalStatus.Completed &&
                ((r.StartDate <= startDate && r.EndDate >= startDate) ||
                 (r.StartDate <= endDate && r.EndDate >= endDate) ||
                 (r.StartDate >= startDate && r.EndDate <= endDate)));

            return !conflictingRentals.Any();
        }

        public async Task<decimal> CalculateRentalPriceAsync(int carId, DateTime startDate, DateTime endDate)
        {
            var car = await _unitOfWork.Cars.GetByIdAsync(carId);
            if (car == null)
            {
                throw new InvalidOperationException("Xe không tồn tại");
            }

            var duration = endDate - startDate;
            var totalHours = duration.TotalHours;
            var totalDays = duration.TotalDays;

            // ✅ THUÊ THEO GIỜ (4-23 giờ)
            if (totalHours >= 4 && totalHours < 24)
            {
                // Validate xe có hỗ trợ thuê theo giờ
                if (!car.PricePerHour.HasValue || car.PricePerHour <= 0)
                {
                    throw new InvalidOperationException("Xe này không hỗ trợ thuê theo giờ");
                }

                // Validate min 4 hours
                if (totalHours < 4)
                {
                    throw new InvalidOperationException("Thuê theo giờ tối thiểu 4 giờ");
                }

                var hours = (int)Math.Ceiling(totalHours);
                var rentalPrice = hours * car.PricePerHour.Value;

                _logger.LogInformation("Calculate hourly rental: {Hours} hours × {PricePerHour} = {TotalPrice}",
                    hours, car.PricePerHour.Value, rentalPrice);

                return rentalPrice;
            }
            // ✅ THUÊ THEO NGÀY (≥24 giờ)
            else
            {
                // Validate min 1 day
                if (totalDays < 1)
                {
                    throw new InvalidOperationException("Thuê theo ngày tối thiểu 1 ngày (24 giờ)");
                }

                var days = Math.Max(1, (int)Math.Ceiling(totalDays));
                var rentalPrice = days * car.PricePerDay;

                _logger.LogInformation("Calculate daily rental: {Days} days × {PricePerDay} = {TotalPrice}",
                    days, car.PricePerDay, rentalPrice);

                return rentalPrice;
            }
        }

        public async Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var rentals = await _unitOfWork.Rentals.GetAsync(r =>
                r.Status == RentalStatus.Completed &&
                (!startDate.HasValue || r.CreatedAt >= startDate.Value) &&
                (!endDate.HasValue || r.CreatedAt <= endDate.Value));

            return rentals.Sum(r => r.TotalPrice + (r.LateFee ?? 0) + (r.DamageFee ?? 0));
        }

        public async Task<IEnumerable<Rental>> GetRentalsInDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _unitOfWork.Rentals.GetAsync(r =>
                r.StartDate.Date >= startDate.Date && r.StartDate.Date <= endDate.Date);
        }

        public async Task<bool> ProcessOverdueRentalsAsync()
        {
            try
            {
                var overdueRentals = await GetOverdueRentalsAsync();

                foreach (var rental in overdueRentals)
                {
                    rental.Status = RentalStatus.Overdue;
                    rental.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Rentals.Update(rental);
                }

                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Đã xử lý {Count} rental quá hạn", overdueRentals.Count());
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý rental quá hạn");
                return false;
            }
        }


    }
}