using carrentalmvc.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace carrentalmvc.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Route("api/customer/[controller]")]
    [ApiController]
    public class DriverFeesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DriverFeesController> _logger;

        public DriverFeesController(
            ApplicationDbContext context,
            ILogger<DriverFeesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// ✅ NEW: Get estimated driver fee (max fee của tất cả drivers)
        /// Dùng để khách hàng biết phí tài xế ước tính khi đặt xe
        /// </summary>
        [HttpGet("estimate/{carId}")]
        public async Task<IActionResult> GetEstimatedDriverFee(int carId)
        {
            try
            {
                // 1. Lấy thông tin xe để biết CarOwnerId
                var car = await _context.Cars
                    .AsNoTracking()
                    .Where(c => c.CarId == carId)
                    .Select(c => new { c.CarId, c.OwnerId })
                    .FirstOrDefaultAsync();

                if (car == null)
                {
                    return NotFound(new { 
                        success = false,
                        hasDrivers = false, 
                        maxDailyFee = 0m,
                        message = "Xe không tồn tại" 
                    });
                }

                // 2. Lấy danh sách phí của tất cả tài xế ACTIVE
                var activeDriverFees = await _context.DriverAssignments
                    .AsNoTracking()
                    .Where(da => da.CarOwnerId == car.OwnerId && da.IsActive)
                    .Select(da => da.DailyDriverFee)
                    .ToListAsync();

                if (!activeDriverFees.Any())
                {
                    _logger.LogInformation("No active drivers for car {CarId} owner {OwnerId}", 
                        carId, car.OwnerId);

                    return Ok(new
                    {
                        success = true,
                        hasDrivers = false,
                        maxDailyFee = 0m,
                        avgDailyFee = 0m,
                        minDailyFee = 0m,
                        driverCount = 0,
                        message = "Chủ xe chưa có tài xế"
                    });
                }

                // 3. Tính toán thống kê
                var maxFee = activeDriverFees.Max();
                var avgFee = activeDriverFees.Average();
                var minFee = activeDriverFees.Min();

                _logger.LogInformation(
                    "Driver fee estimate for car {CarId}: Max={Max}, Avg={Avg}, Min={Min}, Count={Count}",
                    carId, maxFee, avgFee, minFee, activeDriverFees.Count);

                return Ok(new
                {
                    success = true,
                    hasDrivers = true,
                    maxDailyFee = maxFee,
                    avgDailyFee = Math.Round(avgFee, 0),
                    minDailyFee = minFee,
                    maxHourlyFee = Math.Round(maxFee / 8, 0),
                    avgHourlyFee = Math.Round(avgFee / 8, 0),
                    minHourlyFee = Math.Round(minFee / 8, 0),
                    driverCount = activeDriverFees.Count,
                    message = $"Có {activeDriverFees.Count} tài xế"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting estimated driver fee for car {CarId}", carId);
                
                return StatusCode(500, new
                {
                    success = false,
                    hasDrivers = false,
                    maxDailyFee = 0m,
                    message = "Lỗi server khi lấy phí tài xế"
                });
            }
        }

        /// <summary>
        /// Get driver fee for a specific car
        /// </summary>
        /// <param name="carId">Car ID</param>
        /// <returns>Driver fee information</returns>
        [HttpGet("{carId}")]
        public async Task<IActionResult> GetDriverFee(int carId)
        {
            try
            {
                // Find car
                var car = await _context.Cars
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CarId == carId);

                if (car == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Không tìm thấy xe",
                        hasDriver = false,
                        dailyFee = 0,
                        hourlyFee = 0
                    });
                }

                // Find active driver assignment for car owner
                var assignment = await _context.DriverAssignments
                    .AsNoTracking()
                    .Where(da => da.CarOwnerId == car.OwnerId && da.IsActive)
                    .OrderByDescending(da => da.AssignedAt)
                    .Select(da => new
                    {
                        da.DailyDriverFee,
                        HourlyDriverFee = da.DailyDriverFee / 8,
                        da.DriverId,
                        DriverName = da.Driver.FullName
                    })
                    .FirstOrDefaultAsync();

                if (assignment == null)
                {
                    _logger.LogInformation("No active driver found for car {CarId}, owner {OwnerId}", 
                        carId, car.OwnerId);

                    return Ok(new
                    {
                        success = true,
                        hasDriver = false,
                        dailyFee = 0,
                        hourlyFee = 0,
                        message = "Chủ xe chưa có tài xế"
                    });
                }

                _logger.LogInformation("Driver fee found for car {CarId}: Daily={DailyFee}, Hourly={HourlyFee}", 
                    carId, assignment.DailyDriverFee, assignment.HourlyDriverFee);

                return Ok(new
                {
                    success = true,
                    hasDriver = true,
                    dailyFee = assignment.DailyDriverFee,
                    hourlyFee = assignment.HourlyDriverFee,
                    driverId = assignment.DriverId,
                    driverName = assignment.DriverName ?? "Tài xế"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting driver fee for car {CarId}", carId);
                
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi lấy thông tin phí tài xế",
                    hasDriver = false,
                    dailyFee = 0,
                    hourlyFee = 0
                });
            }
        }

        /// <summary>
        /// Get all active drivers for a car owner
        /// </summary>
        /// <param name="ownerId">Car owner user ID</param>
        /// <returns>List of active drivers</returns>
        [HttpGet("owner/{ownerId}")]
        public async Task<IActionResult> GetOwnerDrivers(string ownerId)
        {
            try
            {
                var drivers = await _context.DriverAssignments
                    .AsNoTracking()
                    .Where(da => da.CarOwnerId == ownerId && da.IsActive)
                    .Select(da => new
                    {
                        da.DriverAssignmentId,
                        da.DriverId,
                        DriverName = da.Driver.FullName ?? da.Driver.Email,
                        da.DailyDriverFee,
                        HourlyDriverFee = da.DailyDriverFee / 8,
                        da.AssignedAt
                    })
                    .OrderByDescending(d => d.AssignedAt)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    count = drivers.Count,
                    drivers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drivers for owner {OwnerId}", ownerId);
                
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi lấy danh sách tài xế"
                });
            }
        }
    }
}
