using carrentalmvc.Data.Constants;
using carrentalmvc.Models;
using carrentalmvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace carrentalmvc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleConstants.Admin)]
    public class BrandsController : Controller
    {
        private readonly IBrandService _brandService;
        private readonly ILogger<BrandsController> _logger;

        public BrandsController(IBrandService brandService, ILogger<BrandsController> logger)
        {
            _brandService = brandService;
            _logger = logger;
        }

        // GET: Admin/Brands
        public async Task<IActionResult> Index(string? searchTerm, int page = 1, int pageSize = 10)
        {
            try
            {
                var brands = await _brandService.GetAllBrandsAsync();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    brands = brands.Where(b => b.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                              (!string.IsNullOrEmpty(b.Description) && b.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
                }

                var totalCount = brands.Count();
                var pagedBrands = brands
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Sử dụng ViewBag để truyền dữ liệu phân trang thay vì ViewModel
                ViewBag.SearchTerm = searchTerm;
                ViewBag.PageNumber = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                return View(pagedBrands);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading brands list");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách thương hiệu.";
                return View(new List<Brand>());
            }
        }

        // GET: Admin/Brands/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var brand = await _brandService.GetBrandByIdAsync(id);
                if (brand == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thương hiệu.";
                    return RedirectToAction(nameof(Index));
                }

                // Truyền số lượng xe qua ViewBag
                ViewBag.CarCount = await _brandService.GetCarCountByBrandAsync(id);

                return View(brand);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading brand details for ID: {BrandId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin thương hiệu.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Brands/Create
        public IActionResult Create()
        {
            return View(new Brand());
        }

        // POST: Admin/Brands/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Brand brand)
        {
            if (!ModelState.IsValid)
            {
                return View(brand);
            }

            try
            {
                // Thiết lập thời gian tạo và cập nhật
                brand.CreatedAt = DateTime.UtcNow;
                brand.UpdatedAt = DateTime.UtcNow;

                await _brandService.CreateBrandAsync(brand);

                TempData["SuccessMessage"] = "Thương hiệu đã được tạo thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(brand);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating brand: {BrandName}", brand.Name);
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo thương hiệu.");
                return View(brand);
            }
        }

        // GET: Admin/Brands/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var brand = await _brandService.GetBrandByIdAsync(id);
                if (brand == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thương hiệu.";
                    return RedirectToAction(nameof(Index));
                }

                return View(brand);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading brand for edit: {BrandId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin thương hiệu.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Brands/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Brand brand)
        {
            if (id != brand.BrandId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(brand);
            }

            try
            {
                // Cập nhật thời gian sửa đổi
                brand.UpdatedAt = DateTime.UtcNow;

                await _brandService.UpdateBrandAsync(brand);

                TempData["SuccessMessage"] = "Thương hiệu đã được cập nhật thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(brand);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating brand: {BrandId}", id);
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật thương hiệu.");
                return View(brand);
            }
        }

        // GET: Admin/Brands/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var brand = await _brandService.GetBrandByIdAsync(id);
                if (brand == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thương hiệu.";
                    return RedirectToAction(nameof(Index));
                }

                // Truyền số lượng xe qua ViewBag
                ViewBag.CarCount = await _brandService.GetCarCountByBrandAsync(id);

                return View(brand);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading brand for delete: {BrandId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin thương hiệu.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Brands/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _brandService.DeleteBrandAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Thương hiệu đã được xóa thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thương hiệu để xóa.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting brand: {BrandId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa thương hiệu.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }
    }
}