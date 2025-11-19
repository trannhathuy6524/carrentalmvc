using carrentalmvc.Data.Constants;
using carrentalmvc.Models;
using carrentalmvc.Models.Enums;
using carrentalmvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace carrentalmvc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleConstants.Admin)]
    public class Model3DTemplatesController : Controller
    {
        private readonly IModel3DTemplateService _model3DTemplateService;
        private readonly IBrandService _brandService;
        private readonly ICategoryService _categoryService;
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<Model3DTemplatesController> _logger;

        public Model3DTemplatesController(
            IModel3DTemplateService model3DTemplateService,
            IBrandService brandService,
            ICategoryService categoryService,
            IFileUploadService fileUploadService,
            ILogger<Model3DTemplatesController> logger)
        {
            _model3DTemplateService = model3DTemplateService;
            _brandService = brandService;
            _categoryService = categoryService;
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        // GET: Admin/Model3DTemplates
        public async Task<IActionResult> Index(
            string? searchTerm,
            int? brandId,
            int? categoryId,
            FileFormat? fileFormat,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var templates = await _model3DTemplateService.GetAllTemplatesAsync();

                // Apply filters
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    templates = templates.Where(t => t.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                                    (!string.IsNullOrEmpty(t.Description) && t.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
                }

                if (brandId.HasValue)
                {
                    templates = templates.Where(t => t.BrandId == brandId.Value);
                }

                if (categoryId.HasValue)
                {
                    templates = templates.Where(t => t.CategoryId == categoryId.Value);
                }

                if (fileFormat.HasValue)
                {
                    templates = templates.Where(t => t.FileFormat == fileFormat.Value);
                }

                var totalCount = templates.Count();
                var pagedTemplates = templates
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Sử dụng ViewBag để truyền dữ liệu filter và phân trang
                ViewBag.SearchTerm = searchTerm;
                ViewBag.BrandId = brandId;
                ViewBag.CategoryId = categoryId;
                ViewBag.FileFormat = fileFormat;
                ViewBag.PageNumber = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Load dropdown data cho filter
                await LoadFilterDropdownsAsync(brandId, categoryId);

                // Helper functions cho view
                ViewBag.GetFileFormatText = new Func<FileFormat, string>(GetFileFormatText);
                ViewBag.GetFileSizeText = new Func<long?, string>(GetFileSizeText);

                // Thống kê tổng quan
                var allTemplates = await _model3DTemplateService.GetAllTemplatesAsync();
                ViewBag.TotalTemplates = allTemplates.Count();
                ViewBag.ActiveTemplates = allTemplates.Count(t => t.IsActive);
                ViewBag.InactiveTemplates = allTemplates.Count(t => !t.IsActive);
                ViewBag.TotalFileSize = allTemplates.Where(t => t.FileSize.HasValue).Sum(t => t.FileSize!.Value);

                return View(pagedTemplates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading 3D model templates");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách mô hình 3D.";
                return View(new List<Model3DTemplate>());
            }
        }

        // GET: Admin/Model3DTemplates/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var template = await _model3DTemplateService.GetTemplateByIdAsync(id);
                if (template == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy mô hình 3D.";
                    return RedirectToAction(nameof(Index));
                }

                // Truyền thông tin bổ sung qua ViewBag
                ViewBag.FileFormatText = GetFileFormatText(template.FileFormat);
                ViewBag.FileSizeText = GetFileSizeText(template.FileSize);
                ViewBag.UsageCount = await _model3DTemplateService.GetUsageCountAsync(id);
                ViewBag.BrandName = template.Brand?.Name ?? "Chung";
                ViewBag.CategoryName = template.Category?.Name ?? "Chung";

                return View(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading 3D model template details for ID: {TemplateId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin mô hình 3D.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Model3DTemplates/Create
        public async Task<IActionResult> Create()
        {
            var template = new Model3DTemplate();
            await LoadCreateEditDropdownsAsync();
            return View(template);
        }

        // POST: Admin/Model3DTemplates/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Model3DTemplate template,
            IFormFile? modelFile,
            IFormFile? previewImageFile,
            string? modelUrl,
            string? previewImageUrl,
            long? fileSize)
        {
            // ✅ QUAN TRỌNG: Loại bỏ validation cho ModelUrl vì nó sẽ được set sau khi upload file
            ModelState.Remove("ModelUrl");
            
            // Custom validation for model source
            if (string.IsNullOrEmpty(modelUrl) && modelFile == null)
            {
                ModelState.AddModelError("", "Vui lòng chọn file mô hình 3D hoặc nhập URL mô hình 3D.");
            }

            // Validate model file if uploaded
            if (modelFile != null && !_fileUploadService.IsValidModelFile(modelFile))
            {
                ModelState.AddModelError("ModelFile", "File mô hình 3D không hợp lệ. Định dạng cho phép: GLB, GLTF, OBJ, FBX. Kích thước tối đa: 100MB.");
            }

            // Validate preview image file if uploaded
            if (previewImageFile != null && !_fileUploadService.IsValidImageFile(previewImageFile))
            {
                ModelState.AddModelError("PreviewImageFile", "File ảnh không hợp lệ. Định dạng cho phép: JPG, PNG, GIF, WEBP. Kích thước tối đa: 5MB.");
            }

            if (!ModelState.IsValid)
            {
                await LoadCreateEditDropdownsAsync();
                return View(template);
            }

            try
            {
                // Set timestamps
                template.CreatedAt = DateTime.UtcNow;
                template.UpdatedAt = DateTime.UtcNow;

                // Handle model file upload
                if (modelFile != null)
                {
                    template.ModelUrl = await _fileUploadService.UploadFileAsync(modelFile, "models");
                    template.FileSize = modelFile.Length;
                }
                else
                {
                    template.ModelUrl = modelUrl!;
                    template.FileSize = fileSize;
                }

                // Handle preview image upload
                if (previewImageFile != null)
                {
                    template.PreviewImageUrl = await _fileUploadService.UploadFileAsync(previewImageFile, "previews");
                }
                else if (!string.IsNullOrEmpty(previewImageUrl))
                {
                    template.PreviewImageUrl = previewImageUrl;
                }

                await _model3DTemplateService.CreateTemplateAsync(template);

                TempData["SuccessMessage"] = "Mô hình 3D đã được tạo thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                await LoadCreateEditDropdownsAsync();
                return View(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating 3D model template: {TemplateName}", template.Name);
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo mô hình 3D.");
                await LoadCreateEditDropdownsAsync();
                return View(template);
            }
        }

        // GET: Admin/Model3DTemplates/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var template = await _model3DTemplateService.GetTemplateByIdAsync(id);
                if (template == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy mô hình 3D.";
                    return RedirectToAction(nameof(Index));
                }

                // Truyền thông tin hiện tại qua ViewBag để hiển thị
                ViewBag.CurrentModelUrl = template.ModelUrl;
                ViewBag.CurrentPreviewImageUrl = template.PreviewImageUrl;
                ViewBag.CurrentFileSize = template.FileSize;

                await LoadCreateEditDropdownsAsync();
                return View(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading 3D model template for edit: {TemplateId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin mô hình 3D.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Model3DTemplates/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Model3DTemplate template,
            IFormFile? modelFile,
            IFormFile? previewImageFile,
            string? modelUrl,
            string? previewImageUrl,
            long? fileSize)
        {
            if (id != template.TemplateId)
            {
                return BadRequest();
            }

            try
            {
                var existingTemplate = await _model3DTemplateService.GetTemplateByIdAsync(id);
                if (existingTemplate == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy mô hình 3D.";
                    return RedirectToAction(nameof(Index));
                }

                // ✅ QUAN TRỌNG: Loại bỏ validation cho ModelUrl vì có thể dùng file hoặc URL
                ModelState.Remove("ModelUrl");

                // Custom validation for model source
                if (string.IsNullOrEmpty(modelUrl) && modelFile == null && string.IsNullOrEmpty(existingTemplate.ModelUrl))
                {
                    ModelState.AddModelError("", "Vui lòng chọn file mô hình 3D hoặc nhập URL mô hình 3D.");
                }

                // Validate model file if uploaded
                if (modelFile != null && !_fileUploadService.IsValidModelFile(modelFile))
                {
                    ModelState.AddModelError("ModelFile", "File mô hình 3D không hợp lệ. Định dạng cho phép: GLB, GLTF, OBJ, FBX. Kích thước tối đa: 100MB.");
                }

                // Validate preview image file if uploaded
                if (previewImageFile != null && !_fileUploadService.IsValidImageFile(previewImageFile))
                {
                    ModelState.AddModelError("PreviewImageFile", "File ảnh không hợp lệ. Định dạng cho phép: JPG, PNG, GIF, WEBP. Kích thước tối đa: 5MB.");
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.CurrentModelUrl = existingTemplate.ModelUrl;
                    ViewBag.CurrentPreviewImageUrl = existingTemplate.PreviewImageUrl;
                    ViewBag.CurrentFileSize = existingTemplate.FileSize;
                    await LoadCreateEditDropdownsAsync();
                    return View(template);
                }

                // Update basic properties
                existingTemplate.Name = template.Name;
                existingTemplate.Description = template.Description;
                existingTemplate.FileFormat = template.FileFormat;
                existingTemplate.BrandId = template.BrandId;
                existingTemplate.CategoryId = template.CategoryId;
                existingTemplate.IsActive = template.IsActive;
                existingTemplate.UpdatedAt = DateTime.UtcNow;

                // Handle model file upload
                if (modelFile != null)
                {
                    // Delete old file if it was uploaded
                    if (!string.IsNullOrEmpty(existingTemplate.ModelUrl) && existingTemplate.ModelUrl.StartsWith("/uploads/"))
                    {
                        await _fileUploadService.DeleteFileAsync(existingTemplate.ModelUrl);
                    }

                    existingTemplate.ModelUrl = await _fileUploadService.UploadFileAsync(modelFile, "models");
                    existingTemplate.FileSize = modelFile.Length;
                }
                else if (!string.IsNullOrEmpty(modelUrl))
                {
                    existingTemplate.ModelUrl = modelUrl;
                    existingTemplate.FileSize = fileSize;
                }
                // Keep existing model URL if no new file/URL provided

                // Handle preview image upload
                if (previewImageFile != null)
                {
                    // Delete old file if it was uploaded
                    if (!string.IsNullOrEmpty(existingTemplate.PreviewImageUrl) && existingTemplate.PreviewImageUrl.StartsWith("/uploads/"))
                    {
                        await _fileUploadService.DeleteFileAsync(existingTemplate.PreviewImageUrl);
                    }

                    existingTemplate.PreviewImageUrl = await _fileUploadService.UploadFileAsync(previewImageFile, "previews");
                }
                else if (!string.IsNullOrEmpty(previewImageUrl))
                {
                    existingTemplate.PreviewImageUrl = previewImageUrl;
                }
                // Keep existing preview image URL if no new file/URL provided

                await _model3DTemplateService.UpdateTemplateAsync(existingTemplate);

                TempData["SuccessMessage"] = "Mô hình 3D đã được cập nhật thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.CurrentModelUrl = template.ModelUrl;
                ViewBag.CurrentPreviewImageUrl = template.PreviewImageUrl;
                ViewBag.CurrentFileSize = template.FileSize;
                await LoadCreateEditDropdownsAsync();
                return View(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating 3D model template: {TemplateId}", id);
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật mô hình 3D.");
                ViewBag.CurrentModelUrl = template.ModelUrl;
                ViewBag.CurrentPreviewImageUrl = template.PreviewImageUrl;
                ViewBag.CurrentFileSize = template.FileSize;
                await LoadCreateEditDropdownsAsync();
                return View(template);
            }
        }

        // GET: Admin/Model3DTemplates/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var template = await _model3DTemplateService.GetTemplateByIdAsync(id);
                if (template == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy mô hình 3D.";
                    return RedirectToAction(nameof(Index));
                }

                // Truyền thông tin bổ sung qua ViewBag
                ViewBag.FileFormatText = GetFileFormatText(template.FileFormat);
                ViewBag.FileSizeText = GetFileSizeText(template.FileSize);
                ViewBag.UsageCount = await _model3DTemplateService.GetUsageCountAsync(id);
                ViewBag.BrandName = template.Brand?.Name ?? "Chung";
                ViewBag.CategoryName = template.Category?.Name ?? "Chung";

                return View(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading 3D model template for delete: {TemplateId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin mô hình 3D.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Model3DTemplates/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var template = await _model3DTemplateService.GetTemplateByIdAsync(id);
                if (template != null)
                {
                    // Delete uploaded files
                    if (!string.IsNullOrEmpty(template.ModelUrl) && template.ModelUrl.StartsWith("/uploads/"))
                    {
                        await _fileUploadService.DeleteFileAsync(template.ModelUrl);
                    }
                    if (!string.IsNullOrEmpty(template.PreviewImageUrl) && template.PreviewImageUrl.StartsWith("/uploads/"))
                    {
                        await _fileUploadService.DeleteFileAsync(template.PreviewImageUrl);
                    }
                }

                var result = await _model3DTemplateService.DeleteTemplateAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Mô hình 3D đã được xóa thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy mô hình 3D để xóa.";
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
                _logger.LogError(ex, "Error deleting 3D model template: {TemplateId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa mô hình 3D.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        // POST: Admin/Model3DTemplates/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                await _model3DTemplateService.ToggleActiveStatusAsync(id);
                TempData["SuccessMessage"] = "Trạng thái mô hình 3D đã được cập nhật.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling 3D model template status: {TemplateId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật trạng thái mô hình 3D.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadFilterDropdownsAsync(int? selectedBrandId, int? selectedCategoryId)
        {
            var brands = await _brandService.GetAllBrandsAsync();
            var categories = await _categoryService.GetAllCategoriesAsync();

            ViewBag.Brands = new SelectList(brands, "BrandId", "Name", selectedBrandId);
            ViewBag.Categories = new SelectList(categories, "CategoryId", "Name", selectedCategoryId);
        }

        private async Task LoadCreateEditDropdownsAsync()
        {
            var brands = await _brandService.GetAllBrandsAsync();
            var categories = await _categoryService.GetAllCategoriesAsync();

            ViewBag.Brands = new SelectList(brands, "BrandId", "Name");
            ViewBag.Categories = new SelectList(categories, "CategoryId", "Name");
        }

        private string GetFileFormatText(FileFormat fileFormat)
        {
            return fileFormat switch
            {
                FileFormat.GLB => "GLB",
                FileFormat.GLTF => "GLTF",
                FileFormat.OBJ => "OBJ",
                FileFormat.FBX => "FBX",
                _ => "Unknown"
            };
        }

        private string GetFileSizeText(long? fileSize)
        {
            if (!fileSize.HasValue) return "N/A";

            var size = fileSize.Value;
            if (size < 1024) return $"{size} B";
            if (size < 1024 * 1024) return $"{size / 1024.0:F1} KB";
            if (size < 1024 * 1024 * 1024) return $"{size / (1024.0 * 1024.0):F1} MB";
            return $"{size / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }
    }
}