using carrentalmvc.Data.Constants;
using carrentalmvc.Models;
using carrentalmvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace carrentalmvc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleConstants.Admin)]
    public class CategoriesController : Controller
    {
        private readonly ICategoryService _categoryService;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ICategoryService categoryService, ILogger<CategoriesController> logger)
        {
            _categoryService = categoryService;
            _logger = logger;
        }

        // GET: Admin/Categories
        public async Task<IActionResult> Index(string? searchTerm, int page = 1, int pageSize = 10)
        {
            try
            {
                var categories = await _categoryService.GetAllCategoriesAsync();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    categories = categories.Where(c => c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                                      (!string.IsNullOrEmpty(c.Description) && c.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
                }

                var totalCount = categories.Count();
                var pagedCategories = categories
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Sử dụng ViewBag để truyền dữ liệu phân trang thay vì ViewModel
                ViewBag.SearchTerm = searchTerm;
                ViewBag.PageNumber = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                return View(pagedCategories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories list");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách loại xe.";
                return View(new List<Category>());
            }
        }

        // GET: Admin/Categories/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var category = await _categoryService.GetCategoryByIdAsync(id);
                if (category == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy loại xe.";
                    return RedirectToAction(nameof(Index));
                }

                // Truyền số lượng xe qua ViewBag
                ViewBag.CarCount = await _categoryService.GetCarCountByCategoryAsync(id);

                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading category details for ID: {CategoryId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin loại xe.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Categories/Create
        public IActionResult Create()
        {
            return View(new Category());
        }

        // POST: Admin/Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (!ModelState.IsValid)
            {
                return View(category);
            }

            try
            {
                // Thiết lập thời gian tạo và cập nhật
                category.CreatedAt = DateTime.UtcNow;
                category.UpdatedAt = DateTime.UtcNow;

                await _categoryService.CreateCategoryAsync(category);

                TempData["SuccessMessage"] = "Loại xe đã được tạo thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category: {CategoryName}", category.Name);
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo loại xe.");
                return View(category);
            }
        }

        // GET: Admin/Categories/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var category = await _categoryService.GetCategoryByIdAsync(id);
                if (category == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy loại xe.";
                    return RedirectToAction(nameof(Index));
                }

                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading category for edit: {CategoryId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin loại xe.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.CategoryId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(category);
            }

            try
            {
                // Cập nhật thời gian sửa đổi
                category.UpdatedAt = DateTime.UtcNow;

                await _categoryService.UpdateCategoryAsync(category);

                TempData["SuccessMessage"] = "Loại xe đã được cập nhật thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category: {CategoryId}", id);
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật loại xe.");
                return View(category);
            }
        }

        // GET: Admin/Categories/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var category = await _categoryService.GetCategoryByIdAsync(id);
                if (category == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy loại xe.";
                    return RedirectToAction(nameof(Index));
                }

                // Truyền số lượng xe qua ViewBag
                ViewBag.CarCount = await _categoryService.GetCarCountByCategoryAsync(id);

                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading category for delete: {CategoryId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin loại xe.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _categoryService.DeleteCategoryAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Loại xe đã được xóa thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy loại xe để xóa.";
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
                _logger.LogError(ex, "Error deleting category: {CategoryId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa loại xe.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }
    }
}