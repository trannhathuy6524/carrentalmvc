using carrentalmvc.Data;
using carrentalmvc.Data.Constants;
using carrentalmvc.Helpers;
using carrentalmvc.Models;
using carrentalmvc.Models.Enums;
using carrentalmvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace carrentalmvc.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = RoleConstants.Customer)]
    public class RentalsController : Controller
    {
        private readonly IRentalService _rentalService;
        private readonly ICarService _carService;
        private readonly IPaymentService _paymentService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RentalsController> _logger;
        private readonly ApplicationDbContext _context;

        public RentalsController(
            IRentalService rentalService,
            ICarService carService,
            IPaymentService paymentService,
            UserManager<ApplicationUser> userManager,
            ILogger<RentalsController> logger,
            ApplicationDbContext context)
        {
            _rentalService = rentalService;
            _carService = carService;
            _paymentService = paymentService;
            _userManager = userManager;
            _logger = logger;
            _context = context;
        }

        // GET: Customer/Rentals
        public async Task<IActionResult> Index(RentalStatus? status, int page = 1, int pageSize = 10)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge();
                }

                var myRentals = await _rentalService.GetRentalsByUserAsync(user.Id);

                if (status.HasValue)
                {
                    myRentals = myRentals.Where(r => r.Status == status.Value);
                }

                var totalCount = myRentals.Count();
                var pagedRentals = myRentals
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Pass filter and pagination to ViewBag
                ViewBag.Status = status;
                ViewBag.PageNumber = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                ViewBag.HasPreviousPage = page > 1;
                ViewBag.HasNextPage = page < ViewBag.TotalPages;

                // Helper functions
                ViewBag.GetRentalStatusText = new Func<RentalStatus, string>(GetRentalStatusText);
                ViewBag.CanCancelRental = new Func<Rental, bool>(CanCancelRental);

                return View(pagedRentals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading rentals for customer: {UserId}", User.Identity?.Name);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách thuê xe.";

                ViewBag.PageNumber = 1;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = 0;
                ViewBag.TotalPages = 0;
                ViewBag.HasPreviousPage = false;
                ViewBag.HasNextPage = false;
                ViewBag.GetRentalStatusText = new Func<RentalStatus, string>(GetRentalStatusText);
                ViewBag.CanCancelRental = new Func<Rental, bool>(CanCancelRental);

                return View(new List<Rental>());
            }
        }

        // GET: Customer/Rentals/Create
        public async Task<IActionResult> Create(int carId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var car = await _carService.GetCarWithDetailsAsync(carId);
                
                // ✅ UPDATED: Allow both Available and Rented cars (for future bookings)
                if (car == null || !car.IsActive)
                {
                    TempData["ErrorMessage"] = "Xe không tồn tại.";
                    return RedirectToAction("Index", "Home", new { area = "" });
                }

                // Only block if car is in Maintenance or PendingApproval
                if (car.Status == CarStatus.Maintenance || car.Status == CarStatus.PendingApproval)
                {
                    TempData["ErrorMessage"] = "Xe không khả dụng.";
                    return RedirectToAction("Index", "Home", new { area = "" });
                }

                // Get primary image
                var primaryImage = car.CarImages?.FirstOrDefault(img => img.IsPrimary);
                var carImageUrl = primaryImage?.ImageUrl ?? car.CarImages?.FirstOrDefault()?.ImageUrl;

                // Pass car info to ViewBag
                ViewBag.CarImageUrl = carImageUrl;
                ViewBag.BrandName = car.Brand?.Name ?? "N/A";
                ViewBag.CategoryName = car.Category?.Name ?? "N/A";
                ViewBag.AvailableLocations = GetAvailableLocations();

                // Set default dates if not provided
                ViewBag.DefaultStartDate = startDate ?? DateTime.Today.AddDays(1);
                ViewBag.DefaultEndDate = endDate ?? DateTime.Today.AddDays(3);

                // Pricing constants
                ViewBag.DriverFeePerDay = 500000m; // 500,000 VNĐ/ngày (8 tiếng)

                // Delivery configuration from car owner
                ViewBag.MaxDeliveryDistance = car.MaxDeliveryDistance ?? 50; // Default 50km if not set
                ViewBag.PricePerKmDelivery = car.PricePerKmDelivery ?? 5000m; // Default 5000 VNĐ/km
                ViewBag.DeliveryBaseFee = 50000m; // Base fee for ≤5km

                // Check if car has delivery service
                ViewBag.HasDeliveryService = car.MaxDeliveryDistance.HasValue && car.MaxDeliveryDistance > 0;
                ViewBag.DeliveryServiceNote = ViewBag.HasDeliveryService
                    ? $"Chủ xe giao xe trong phạm vi {car.MaxDeliveryDistance}km với giá {(car.PricePerKmDelivery ?? 0):N0} VNĐ/km"
                    : "Chủ xe không cung cấp dịch vụ giao xe tận nơi";

                // ✅ ADD: Warning message for rented cars
                if (car.Status == CarStatus.Rented)
                {
                    ViewBag.IsRentedCar = true;
                    ViewBag.RentedWarning = "Xe này đang được thuê. Bạn có thể đặt trước cho các ngày sau khi xe trống lịch.";
                }
                else
                {
                    ViewBag.IsRentedCar = false;
                }

                // Helper functions
                ViewBag.GetCarStatusText = new Func<CarStatus, string>(GetCarStatusText);

                // Log for debugging
                _logger.LogInformation("Create rental for car {CarId}: Status={Status}, Location={Location}, Coords=({Lat},{Lng}), MaxDelivery={MaxDist}km, PricePerKm={PriceKm}",
                    car.CarId, car.Status, car.Location, car.Latitude, car.Longitude, car.MaxDeliveryDistance, car.PricePerKmDelivery);

                return View(car);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create rental form for car: {CarId}", carId);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải form đặt thuê xe.";
                return RedirectToAction("Index", "Home", new { area = "" });
            }
        }

        // POST: Customer/Rentals/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int carId,
            DateTime startDate,
            DateTime endDate,
            int serviceType,
            string? customerAddress,
            double? customerLatitude,
            double? customerLongitude,
            string? returnLocation,
            double? returnLatitude,
            double? returnLongitude,
            string? contactPhone,
            string? notes)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge();
                }

                var car = await _carService.GetCarWithDetailsAsync(carId);
                
                // ✅ UPDATED: Allow both Available and Rented cars (for future bookings)
                if (car == null || !car.IsActive)
                {
                    TempData["ErrorMessage"] = "Xe không tồn tại.";
                    return RedirectToAction(nameof(Create), new { carId });
                }

                // Only block if car is in Maintenance or PendingApproval
                if (car.Status == CarStatus.Maintenance || car.Status == CarStatus.PendingApproval)
                {
                    TempData["ErrorMessage"] = "Xe không khả dụng.";
                    return RedirectToAction(nameof(Create), new { carId });
                }

                // ✅ VALIDATE RENTAL DURATION
                var duration = endDate - startDate;
                var totalHours = duration.TotalHours;
                var totalDays = duration.TotalDays;

                bool isHourlyRental = false;
                decimal rentalPrice = 0m;

                // ✅ THUÊ THEO GIỜ (4-23 giờ)
                if (totalHours >= 4 && totalHours < 24)
                {
                    isHourlyRental = true;

                    // Validate xe có hỗ trợ thuê theo giờ không
                    if (!car.PricePerHour.HasValue || car.PricePerHour <= 0)
                    {
                        TempData["ErrorMessage"] = "Xe này không hỗ trợ thuê theo giờ. Vui lòng chọn thuê theo ngày (tối thiểu 1 ngày).";
                        return RedirectToAction(nameof(Create), new { carId, startDate, endDate });
                    }

                    // Validate min 4 hours
                    if (totalHours < 4)
                    {
                        TempData["ErrorMessage"] = "Thuê theo giờ tối thiểu 4 giờ.";
                        return RedirectToAction(nameof(Create), new { carId, startDate, endDate });
                    }

                    // Calculate hourly rental price
                    var hours = (int)Math.Ceiling(totalHours);
                    rentalPrice = hours * car.PricePerHour.Value;

                    _logger.LogInformation("Hourly rental: {Hours} hours × {PricePerHour} = {TotalPrice}",
                        hours, car.PricePerHour.Value, rentalPrice);
                }
                // ✅ THUÊ THEO NGÀY (≥24 giờ hoặc < 4 giờ)
                else
                {
                    // Validate min 1 day
                    if (totalDays < 1)
                    {
                        TempData["ErrorMessage"] = "Thuê theo ngày tối thiểu 1 ngày (24 giờ). Nếu cần thuê ngắn hạn, vui lòng chọn thuê theo giờ (4-23 giờ).";
                        return RedirectToAction(nameof(Create), new { carId, startDate, endDate });
                    }

                    // Calculate daily rental price
                    var days = Math.Max(1, (int)Math.Ceiling(totalDays));
                    rentalPrice = days * car.PricePerDay;

                    _logger.LogInformation("Daily rental: {Days} days × {PricePerDay} = {TotalPrice}",
                        days, car.PricePerDay, rentalPrice);
                }

                // Validate customer address
                if (!customerLatitude.HasValue || !customerLongitude.HasValue)
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn địa chỉ nhận xe trên bản đồ.";
                    return RedirectToAction(nameof(Create), new { carId, startDate, endDate });
                }

                // Validate return location
                if (!returnLatitude.HasValue || !returnLongitude.HasValue)
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn vị trí trả xe trên bản đồ.";
                    return RedirectToAction(nameof(Create), new { carId, startDate, endDate });
                }

                // Calculate distance from car location to customer address
                var carLat = car.Latitude ?? 21.0285;
                var carLng = car.Longitude ?? 105.8542;

                var distance = GeoHelper.CalculateDistance(
                    carLat,
                    carLng,
                    customerLatitude.Value,
                    customerLongitude.Value
                );

                // Calculate delivery fee based on car owner's configuration
                decimal deliveryFee = 0m;

                if (car.MaxDeliveryDistance.HasValue && car.MaxDeliveryDistance > 0)
                {
                    // Check if distance is within max delivery range
                    if (distance > car.MaxDeliveryDistance.Value)
                    {
                        TempData["ErrorMessage"] = $"Khoảng cách giao xe ({distance:F1} km) vượt qua phạm vi tối đa của chủ xe ({car.MaxDeliveryDistance} km). Vui lòng chọn địa điểm gần hơn.";
                        return RedirectToAction(nameof(Create), new { carId, startDate, endDate });
                    }

                    // Calculate fee based on owner's price per km
                    var pricePerKm = car.PricePerKmDelivery ?? 5000m;

                    if (distance <= 5)
                    {
                        // Base fee for distances ≤ 5km
                        deliveryFee = 50000m;
                    }
                    else
                    {
                        // Base fee + additional km * price per km
                        var extraKm = distance - 5;
                        deliveryFee = 50000m + (decimal)(extraKm * (double)pricePerKm);
                    }
                }
                else
                {
                    // If owner doesn't offer delivery, use default calculation
                    deliveryFee = GeoHelper.CalculateDeliveryFee(distance);
                }

                // Check for rental conflicts
                var existingRentals = await _rentalService.GetRentalsByCarAsync(carId);
                var hasConflict = existingRentals.Any(r =>
                    (r.Status == RentalStatus.Pending || r.Status == RentalStatus.Confirmed || r.Status == RentalStatus.Active) &&
                    ((startDate >= r.StartDate && startDate < r.EndDate) ||
                     (endDate > r.StartDate && endDate <= r.EndDate) ||
                     (startDate <= r.StartDate && endDate >= r.EndDate)));

                if (hasConflict)
                {
                    TempData["ErrorMessage"] = "Xe đã được đặt trong khoảng thời gian này.";
                    return RedirectToAction(nameof(Create), new { carId, startDate, endDate });
                }

                // Calculate driver fee
                decimal driverFee = 0m;
                
                if (serviceType == 2) // Có tài xế
                {
                    if (isHourlyRental)
                    {
                        var hours = (int)Math.Ceiling(totalHours);
                        driverFee = hours * 62500m; // 500,000 / 8 = 62,500 VNĐ/giờ
                    }
                    else
                    {
                        var days = Math.Max(1, (int)Math.Ceiling(totalDays));
                        driverFee = days * 500000m; // 500,000 VNĐ/ngày
                    }
                }

                // Calculate total price
                var totalPrice = rentalPrice + driverFee + deliveryFee;

                // ✅ CREATE DETAILED NOTES
                var rentalType = isHourlyRental ? "Thuê theo giờ" : "Thuê theo ngày";
                var durationText = isHourlyRental 
                    ? $"{(int)Math.Ceiling(totalHours)} giờ" 
                    : $"{Math.Max(1, (int)Math.Ceiling(totalDays))} ngày";

                var rentalNotes = $"** LOẠI THUÊ: {rentalType} ({durationText}) **\n";
                rentalNotes += $"Loại dịch vụ: {(serviceType == 1 ? "Tự lái" : "Có tài xế")}\n";
                
                if (isHourlyRental)
                {
                    rentalNotes += $"Giá thuê: {(int)Math.Ceiling(totalHours)} giờ × {car.PricePerHour:N0} VNĐ/giờ = {rentalPrice:N0} VNĐ\n";
                    if (serviceType == 2)
                    {
                        rentalNotes += $"Phí tài xế: {(int)Math.Ceiling(totalHours)} giờ × 62,500 VNĐ/giờ = {driverFee:N0} VNĐ\n";
                    }
                }
                else
                {
                    var days = Math.Max(1, (int)Math.Ceiling(totalDays));
                    rentalNotes += $"Giá thuê: {days} ngày × {car.PricePerDay:N0} VNĐ/ngày = {rentalPrice:N0} VNĐ\n";
                    if (serviceType == 2)
                    {
                        rentalNotes += $"Phí tài xế: {days} ngày × 500,000 VNĐ/ngày = {driverFee:N0} VNĐ\n";
                    }
                }

                rentalNotes += $"\n** GIAO XE TẬN NƠI **\n";
                rentalNotes += $"Vị trí xe: {car.Location ?? "Chưa cập nhật"}\n";
                rentalNotes += $"Tọa độ xe: {carLat:F6}, {carLng:F6}\n";
                rentalNotes += $"Địa chỉ giao xe: {customerAddress ?? "Chưa cung cấp"}\n";
                rentalNotes += $"Tọa độ giao xe: {customerLatitude:F6}, {customerLongitude:F6}\n";
                rentalNotes += $"Khoảng cách giao xe: {distance:F2} km\n";
                rentalNotes += $"Phí giao xe: {deliveryFee:N0} VNĐ";

                if (car.MaxDeliveryDistance.HasValue && car.PricePerKmDelivery.HasValue)
                {
                    rentalNotes += $" (Cấu hình chủ xe: {car.PricePerKmDelivery:N0} VNĐ/km, tối đa {car.MaxDeliveryDistance}km)\n";
                }
                else
                {
                    rentalNotes += " (Phí mặc định)\n";
                }

                rentalNotes += $"Địa điểm trả xe: {returnLocation ?? "Chưa cung cấp"}\n";
                rentalNotes += $"Tọa độ trả xe: {returnLatitude:F6}, {returnLongitude:F6}\n";

                if (!string.IsNullOrEmpty(contactPhone))
                    rentalNotes += $"SĐT liên hệ: {contactPhone}\n";

                if (!string.IsNullOrEmpty(notes))
                    rentalNotes += $"Ghi chú: {notes}";

                var rental = new Rental
                {
                    CarId = carId,
                    RenterId = user.Id,
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalPrice = totalPrice,
                    Status = RentalStatus.Pending,
                    Notes = rentalNotes,
                    RequiresDriver = serviceType == 2,
                    DriverAccepted = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var rentalId = await _rentalService.CreateRentalAsync(rental);

                var successMsg = isHourlyRental
                    ? $"Đặt thuê xe theo giờ thành công! {durationText} - Tổng: {totalPrice:N0} VNĐ"
                    : $"Đặt thuê xe theo ngày thành công! {durationText} - Tổng: {totalPrice:N0} VNĐ";

                TempData["SuccessMessage"] = successMsg;

                return RedirectToAction(nameof(Details), new { id = rentalId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating rental for car: {CarId}", carId);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi đặt thuê xe.";
                return RedirectToAction(nameof(Create), new { carId, startDate, endDate });
            }
        }

        // GET: Customer/Rentals/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var rental = await _rentalService.GetRentalWithDetailsAsync(id);

                if (rental == null || rental.RenterId != user?.Id)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn thuê hoặc bạn không có quyền xem đơn thuê này.";
                    return RedirectToAction(nameof(Index));
                }

                var payments = await _paymentService.GetPaymentsByRentalAsync(id);
                var completedPayments = payments.Where(p => p.Status == PaymentStatus.Completed);
                var totalPaid = completedPayments.Sum(p => p.Amount);
                var remainingAmount = rental.TotalPrice - totalPaid;
                var hasPendingPayments = payments.Any(p => p.Status == PaymentStatus.Pending);

                // ✅ THÊM: Tính toán thông tin đặt cọc
                var depositRequired = rental.TotalPrice * 0.3m; // 30% đặt cọc
                var depositPaid = Math.Min(totalPaid, depositRequired);

                // ✅ THÊM: Phân tích chi phí từ Notes
                var totalDays = (int)(rental.EndDate - rental.StartDate).TotalDays;
                var carRentalFee = totalDays * (rental.Car?.PricePerDay ?? 0);
                
                // Parse từ Notes để lấy thông tin chi tiết
                var serviceType = "Tự lái";
                var driverFee = 0m;
                var deliveryFee = 0m;
                var deliveryDistance = 0.0;

                if (!string.IsNullOrEmpty(rental.Notes))
                {
                    // Parse service type
                    if (rental.Notes.Contains("Có tài xế"))
                    {
                        serviceType = "Có tài xế";
                        
                        // ✅ FIX: Parse driver fee from Notes instead of hardcoding
                        var driverFeeMatch = System.Text.RegularExpressions.Regex.Match(
                            rental.Notes, 
                            @"Phí tài xế: [\d,]+ (?:ngày|giờ) × ([\d,]+) VNĐ/(?:ngày|giờ) = ([\d,]+) VNĐ"
                        );
                        
                        if (driverFeeMatch.Success)
                        {
                            var totalFeeStr = driverFeeMatch.Groups[2].Value.Replace(",", "");
                            decimal.TryParse(totalFeeStr, out driverFee);
                        }
                        else
                        {
                            // Fallback: Calculate based on rental type
                            if (rental.Notes.Contains("Thuê theo giờ"))
                            {
                                var hoursMatch = System.Text.RegularExpressions.Regex.Match(
                                    rental.Notes, 
                                    @"Thuê theo giờ \((\d+) giờ\)"
                                );
                                if (hoursMatch.Success && int.TryParse(hoursMatch.Groups[1].Value, out var hours))
                                {
                                    driverFee = hours * 62500m; // 500k / 8 = 62,500 VNĐ/giờ
                                }
                            }
                            else
                            {
                                // Daily rental
                                driverFee = totalDays * 500000m; // ✅ FIXED: 500,000 VNĐ/ngày
                            }
                        }
                    }

                    // Parse delivery fee
                    var deliveryFeeMatch = System.Text.RegularExpressions.Regex.Match(rental.Notes, @"Phí giao xe: ([\d,]+) VNĐ");
                    if (deliveryFeeMatch.Success)
                    {
                        var feeStr = deliveryFeeMatch.Groups[1].Value.Replace(",", "");
                        decimal.TryParse(feeStr, out deliveryFee);
                    }

                    // Parse delivery distance
                    var distanceMatch = System.Text.RegularExpressions.Regex.Match(rental.Notes, @"Khoảng cách giao xe: ([\d.]+) km");
                    if (distanceMatch.Success)
                    {
                        double.TryParse(distanceMatch.Groups[1].Value, out deliveryDistance);
                    }
                }

                // Pass additional data to ViewBag
                ViewBag.Payments = payments.ToList();
                ViewBag.TotalPaid = totalPaid;
                ViewBag.RemainingAmount = remainingAmount;
                ViewBag.HasPendingPayments = hasPendingPayments;
                ViewBag.TotalDays = totalDays;
                ViewBag.CanCancel = CanCancelRental(rental);

                // ✅ THÊM: Thông tin đặt cọc
                ViewBag.DepositRequired = depositRequired;
                ViewBag.DepositPaid = depositPaid;

                // ✅ THÊM: Chi tiết giá
                ViewBag.CarRentalFee = carRentalFee;
                ViewBag.ServiceType = serviceType;
                ViewBag.DriverFee = driverFee;
                ViewBag.DeliveryFee = deliveryFee;
                ViewBag.DeliveryDistance = deliveryDistance;

                // Helper functions
                ViewBag.GetRentalStatusText = new Func<RentalStatus, string>(GetRentalStatusText);
                ViewBag.GetCarStatusText = new Func<CarStatus, string>(GetCarStatusText);
                ViewBag.GetPaymentMethodText = new Func<PaymentMethod, string>(GetPaymentMethodText);
                ViewBag.GetPaymentStatusText = new Func<PaymentStatus, string>(GetPaymentStatusText);

                return View(rental);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading rental details for ID: {RentalId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin đơn thuê.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Customer/Rentals/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? reason)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var rental = await _rentalService.GetRentalByIdAsync(id);

                if (rental == null || rental.RenterId != user?.Id)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn thuê hoặc bạn không có quyền hủy đơn thuê này.";
                    return RedirectToAction(nameof(Index));
                }

                if (rental.Status != RentalStatus.Pending && rental.Status != RentalStatus.Confirmed)
                {
                    TempData["ErrorMessage"] = "Chỉ có thể hủy đơn thuê ở trạng thái 'Chờ xác nhận' hoặc 'Đã xác nhận'.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var timeUntilStart = rental.StartDate - DateTime.Now;
                if (timeUntilStart.TotalHours < 24)
                {
                    TempData["ErrorMessage"] = "Chỉ có thể hủy đơn thuê trước ít nhất 24 giờ so với thời gian bắt đầu.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var result = await _rentalService.CancelRentalAsync(id, reason ?? "Khách hàng yêu cầu hủy");
                if (result)
                {
                    TempData["SuccessMessage"] = "Đơn thuê đã được hủy thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể hủy đơn thuê này.";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling rental: {RentalId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi hủy đơn thuê.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        #region Private Helper Methods

        private List<string> GetAvailableLocations()
        {
            return new List<string>
            {
                "Hà Nội", "TP. Hồ Chí Minh", "Đà Nẵng", "Hải Phòng",
                "Cần Thơ", "Nha Trang", "Đà Lạt", "Vũng Tàu",
                "Huế", "Quy Nhơn", "Phan Thiết", "Sa Pa"
            };
        }

        private bool CanCancelRental(Rental rental)
        {
            if (rental.Status != RentalStatus.Pending && rental.Status != RentalStatus.Confirmed)
                return false;

            var timeUntilStart = rental.StartDate - DateTime.Now;
            return timeUntilStart.TotalHours >= 24;
        }

        private string GetRentalStatusText(RentalStatus status)
        {
            return status switch
            {
                RentalStatus.Pending => "Chờ xác nhận",
                RentalStatus.Confirmed => "Đã xác nhận",
                RentalStatus.Active => "Đang thuê",
                RentalStatus.Completed => "Hoàn thành",
                RentalStatus.Cancelled => "Đã hủy",
                RentalStatus.Overdue => "Quá hạn",
                _ => "Không xác định"
            };
        }

        private string GetCarStatusText(CarStatus status)
        {
            return status switch
            {
                CarStatus.Available => "Sẵn sàng",
                CarStatus.Rented => "Đang thuê",
                CarStatus.Maintenance => "Bảo trì",
                CarStatus.PendingApproval => "Chờ duyệt",
                CarStatus.Reserved => "Đã đặt",
                _ => "Không xác định"
            };
        }

        private string GetPaymentStatusText(PaymentStatus status)
        {
            return status switch
            {
                PaymentStatus.Pending => "Chờ thanh toán",
                PaymentStatus.Completed => "Đã thanh toán",
                PaymentStatus.Failed => "Thất bại",
                PaymentStatus.Cancelled => "Đã hủy",
                _ => "Không xác định"
            };
        }

        private string GetPaymentMethodText(PaymentMethod method)
        {
            return method switch
            {
                PaymentMethod.Cash => "Tiền mặt",
                PaymentMethod.BankTransfer => "Chuyển khoản",
                PaymentMethod.CreditCard => "Thẻ tín dụng",
                PaymentMethod.EWallet => "Ví điện tử",
                _ => "Khác"
            };
        }

        #endregion
    }
}
