using carrentalmvc.Data.Constants;
using carrentalmvc.Models;
using carrentalmvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace carrentalmvc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleConstants.Admin)]
    public class FeaturesController : Controller
    {
        private readonly IFeatureService _featureService;
        private readonly ILogger<FeaturesController> _logger;

        public FeaturesController(IFeatureService featureService, ILogger<FeaturesController> logger)
        {
            _featureService = featureService;
            _logger = logger;
        }

        // GET: Admin/Features
        public async Task<IActionResult> Index(string? searchTerm, int page = 1, int pageSize = 10)
        {
            try
            {
                var features = await _featureService.GetAllFeaturesAsync();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    features = features.Where(f => f.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                                  (!string.IsNullOrEmpty(f.Description) && f.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
                }

                var totalCount = features.Count();
                var pagedFeatures = features
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Sử dụng ViewBag để truyền dữ liệu phân trang thay vì ViewModel
                ViewBag.SearchTerm = searchTerm;
                ViewBag.PageNumber = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                return View(pagedFeatures);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading features list");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách tính năng.";
                return View(new List<Feature>());
            }
        }

        // GET: Admin/Features/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var feature = await _featureService.GetFeatureByIdAsync(id);
                if (feature == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy tính năng.";
                    return RedirectToAction(nameof(Index));
                }

                return View(feature);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading feature details for ID: {FeatureId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin tính năng.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Features/Create
        public IActionResult Create()
        {
            return View(new Feature());
        }

        // POST: Admin/Features/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Feature feature)
        {
            if (!ModelState.IsValid)
            {
                return View(feature);
            }

            try
            {
                // Thiết lập thời gian tạo và cập nhật
                feature.CreatedAt = DateTime.UtcNow;
                feature.UpdatedAt = DateTime.UtcNow;

                await _featureService.CreateFeatureAsync(feature);

                TempData["SuccessMessage"] = "Tính năng đã được tạo thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(feature);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating feature: {FeatureName}", feature.Name);
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo tính năng.");
                return View(feature);
            }
        }

        // GET: Admin/Features/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var feature = await _featureService.GetFeatureByIdAsync(id);
                if (feature == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy tính năng.";
                    return RedirectToAction(nameof(Index));
                }

                return View(feature);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading feature for edit: {FeatureId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin tính năng.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Features/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Feature feature)
        {
            if (id != feature.FeatureId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(feature);
            }

            try
            {
                // Cập nhật thời gian sửa đổi
                feature.UpdatedAt = DateTime.UtcNow;

                await _featureService.UpdateFeatureAsync(feature);

                TempData["SuccessMessage"] = "Tính năng đã được cập nhật thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(feature);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating feature: {FeatureId}", id);
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật tính năng.");
                return View(feature);
            }
        }

        // GET: Admin/Features/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var feature = await _featureService.GetFeatureByIdAsync(id);
                if (feature == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy tính năng.";
                    return RedirectToAction(nameof(Index));
                }

                return View(feature);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading feature for delete: {FeatureId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin tính năng.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Features/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _featureService.DeleteFeatureAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Tính năng đã được xóa thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy tính năng để xóa.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting feature: {FeatureId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa tính năng.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }
    }
}