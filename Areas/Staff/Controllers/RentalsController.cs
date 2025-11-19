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
    public class RentalsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RentalsController> _logger;

        public RentalsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<RentalsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Staff/Rentals
        public async Task<IActionResult> Index(RentalStatus? status, string searchTerm)
        {
            var query = _context.Rentals
                .Include(r => r.Car)
                    .ThenInclude(c => c.Brand)
                .Include(r => r.Renter)
                .Include(r => r.Payments)
                .AsQueryable();

            // Status filter
            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
                ViewBag.Status = status.Value;
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(r => r.Car.Name.Contains(searchTerm) ||
                                        r.Renter.FullName.Contains(searchTerm) ||
                                        r.RentalId.ToString().Contains(searchTerm));
                ViewBag.SearchTerm = searchTerm;
            }

            var rentals = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Statistics
            ViewBag.TotalRentals = await _context.Rentals.CountAsync();
            ViewBag.PendingRentals = await _context.Rentals.CountAsync(r => r.Status == RentalStatus.Pending);
            ViewBag.ActiveRentals = await _context.Rentals.CountAsync(r => r.Status == RentalStatus.Active);
            ViewBag.CompletedRentals = await _context.Rentals.CountAsync(r => r.Status == RentalStatus.Completed);

            return View(rentals);
        }

        // GET: Staff/Rentals/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rental = await _context.Rentals
                .Include(r => r.Car)
                    .ThenInclude(c => c.Brand)
                .Include(r => r.Car)
                    .ThenInclude(c => c.Category)
                .Include(r => r.Car)
                    .ThenInclude(c => c.Owner)
                .Include(r => r.Car)
                    .ThenInclude(c => c.CarImages)
                .Include(r => r.Renter)
                .Include(r => r.Payments)
                .Include(r => r.Driver)
                .FirstOrDefaultAsync(r => r.RentalId == id);

            if (rental == null)
            {
                return NotFound();
            }

            return View(rental);
        }
    }
}
