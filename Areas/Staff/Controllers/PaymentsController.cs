using carrentalmvc.Data;
using carrentalmvc.Data.Constants;
using carrentalmvc.Models;
using carrentalmvc.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace carrentalmvc.Areas.Staff.Controllers
{
    [Area("Staff")]
    [Authorize(Roles = RoleConstants.Staff)]
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<PaymentsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Staff/Payments
        public async Task<IActionResult> Index(PaymentStatus? status, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Payments
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Car)
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Renter)
                .AsQueryable();

            // Status filter
            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
                ViewBag.Status = status.Value;
            }

            // Date range filter
            if (fromDate.HasValue)
            {
                query = query.Where(p => p.CreatedAt >= fromDate.Value);
                ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.AddDays(1);
                query = query.Where(p => p.CreatedAt < endDate);
                ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            }

            var payments = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // Statistics
            ViewBag.TotalPayments = await _context.Payments.CountAsync();
            ViewBag.CompletedPayments = await _context.Payments.CountAsync(p => p.Status == PaymentStatus.Completed);
            ViewBag.TotalAmount = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Completed)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            return View(payments);
        }

        // GET: Staff/Payments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Car)
                        .ThenInclude(c => c.Brand)
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Renter)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }
    }
}
