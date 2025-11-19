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
    public class CarsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CarsController> _logger;

        public CarsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<CarsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Staff/Cars
        public async Task<IActionResult> Index(string searchTerm, int? brandId, int? categoryId)
        {
            var query = _context.Cars
                .Include(c => c.Brand)
                .Include(c => c.Category)
                .Include(c => c.Owner)
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c => c.Name.Contains(searchTerm) || 
                                        c.Description.Contains(searchTerm) ||
                                        c.LicensePlate.Contains(searchTerm));
                ViewBag.SearchTerm = searchTerm;
            }

            // Brand filter
            if (brandId.HasValue)
            {
                query = query.Where(c => c.BrandId == brandId.Value);
                ViewBag.BrandId = brandId.Value;
            }

            // Category filter
            if (categoryId.HasValue)
            {
                query = query.Where(c => c.CategoryId == categoryId.Value);
                ViewBag.CategoryId = categoryId.Value;
            }

            var cars = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            // For filters
            ViewBag.Brands = await _context.Brands.OrderBy(b => b.Name).ToListAsync();
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();

            return View(cars);
        }

        // GET: Staff/Cars/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var car = await _context.Cars
                .Include(c => c.Brand)
                .Include(c => c.Category)
                .Include(c => c.Owner)
                .Include(c => c.CarImages)
                .Include(c => c.CarFeatures)
                    .ThenInclude(cf => cf.Feature)
                .Include(c => c.CarModel3D)
                .FirstOrDefaultAsync(c => c.CarId == id);

            if (car == null)
            {
                return NotFound();
            }

            // Get rental history
            var rentals = await _context.Rentals
                .Include(r => r.Renter)
                .Where(r => r.CarId == id)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .ToListAsync();

            ViewBag.Rentals = rentals;

            return View(car);
        }
    }
}
