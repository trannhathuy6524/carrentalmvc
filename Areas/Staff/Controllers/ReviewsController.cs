using carrentalmvc.Data;
using carrentalmvc.Data.Constants;
using carrentalmvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace carrentalmvc.Areas.Staff.Controllers
{
    [Area("Staff")]
    [Authorize(Roles = RoleConstants.Staff)]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ReviewsController> _logger;

        public ReviewsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ReviewsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Staff/Reviews
        public async Task<IActionResult> Index(int? rating, string searchTerm)
        {
            var query = _context.Reviews
                .Include(r => r.Car)
                    .ThenInclude(c => c.Brand)
                .Include(r => r.User)
                .AsQueryable();

            // Rating filter
            if (rating.HasValue)
            {
                query = query.Where(r => r.Rating == rating.Value);
                ViewBag.Rating = rating.Value;
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(r => r.Car.Name.Contains(searchTerm) ||
                                        r.User.FullName.Contains(searchTerm) ||
                                        r.Comment.Contains(searchTerm));
                ViewBag.SearchTerm = searchTerm;
            }

            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Statistics
            ViewBag.TotalReviews = await _context.Reviews.CountAsync();
            ViewBag.AverageRating = await _context.Reviews.AverageAsync(r => (double?)r.Rating) ?? 0;

            return View(reviews);
        }

        // GET: Staff/Reviews/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews
                .Include(r => r.Car)
                    .ThenInclude(c => c.Brand)
                .Include(r => r.Car)
                    .ThenInclude(c => c.CarImages)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.ReviewId == id);

            if (review == null)
            {
                return NotFound();
            }

            return View(review);
        }
    }
}
