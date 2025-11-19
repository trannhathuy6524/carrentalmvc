using carrentalmvc.Data.Constants;
using carrentalmvc.Models;
using carrentalmvc.Models.Enums;
using carrentalmvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace carrentalmvc.Areas.CarOwner.Controllers
{
    [Area("CarOwner")]
    [Authorize(Roles = RoleConstants.Owner)]
    public class CarsController : Controller
    {
        private readonly ICarService _carService;
        private readonly IBrandService _brandService;
        private readonly ICategoryService _categoryService;
        private readonly IFeatureService _featureService;
        private readonly IModel3DTemplateService _model3DTemplateService;
        private readonly IRentalService _rentalService;
        private readonly IReviewService _reviewService;
        private readonly IFileUploadService _fileUploadService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CarsController> _logger;

        public CarsController(
            ICarService carService,
            IBrandService brandService,
            ICategoryService categoryService,
            IFeatureService featureService,
            IModel3DTemplateService model3DTemplateService,
            IRentalService rentalService,
            IReviewService reviewService,
            IFileUploadService fileUploadService,
            UserManager<ApplicationUser> userManager,
            ILogger<CarsController> logger)
        {
            _carService = carService;
            _brandService = brandService;
            _categoryService = categoryService;
            _featureService = featureService;
            _model3DTemplateService = model3DTemplateService;
            _rentalService = rentalService;
            _reviewService = reviewService;
            _fileUploadService = fileUploadService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: CarOwner/Cars
        public async Task<IActionResult> Index(
            string? searchTerm,
            CarStatus? status,
            int? brandId,
            int? categoryId,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge();
                }

                var myCars = await _carService.GetCarsByOwnerAsync(user.Id);

                // Apply filters
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    myCars = myCars.Where(c => c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
                }

                if (status.HasValue)
                {
                    myCars = myCars.Where(c => c.Status == status.Value);
                }

                if (brandId.HasValue)
                {
                    myCars = myCars.Where(c => c.BrandId == brandId.Value);
                }

                if (categoryId.HasValue)
                {
                    myCars = myCars.Where(c => c.CategoryId == categoryId.Value);
                }

                var totalCount = myCars.Count();
                var pagedCars = myCars
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Load dropdown data for filters
                var brands = await _brandService.GetAllBrandsAsync();
                var categories = await _categoryService.GetAllCategoriesAsync();

                // Pass filter data via ViewBag
                ViewBag.SearchTerm = searchTerm;
                ViewBag.Status = status;
                ViewBag.BrandId = brandId;
                ViewBag.CategoryId = categoryId;
                ViewBag.Brands = new SelectList(brands, "BrandId", "Name", brandId);
                ViewBag.Categories = new SelectList(categories, "CategoryId", "Name", categoryId);

                // Pagination
                ViewBag.PageNumber = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Helper functions
                ViewBag.GetCarStatusText = new Func<CarStatus, string>(GetCarStatusText);
                ViewBag.GetTransmissionText = new Func<Transmission?, string?>(GetTransmissionText);
                ViewBag.GetFuelTypeText = new Func<FuelType?, string?>(GetFuelTypeText);

                return View(pagedCars);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cars for owner: {UserId}", User.Identity?.Name);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách xe.";
                return View(new List<Car>());
            }
        }

        // GET: CarOwner/Cars/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var car = await _carService.GetCarWithDetailsAsync(id);

                if (car == null || car.OwnerId != user?.Id)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy xe hoặc bạn không có quyền xem xe này.";
                    return RedirectToAction(nameof(Index));
                }

                // Pass additional statistics via ViewBag
                var rentals = await _rentalService.GetRentalsByCarAsync(id);
                var reviews = await _reviewService.GetReviewsByCarAsync(id);

                ViewBag.TotalRentals = rentals.Count();
                ViewBag.ActiveRentals = rentals.Count(r => r.Status == RentalStatus.Active);
                ViewBag.AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
                ViewBag.ReviewCount = reviews.Count();

                // Helper functions
                ViewBag.GetCarStatusText = new Func<CarStatus, string>(GetCarStatusText);
                ViewBag.GetTransmissionText = new Func<Transmission?, string?>(GetTransmissionText);
                ViewBag.GetFuelTypeText = new Func<FuelType?, string?>(GetFuelTypeText);
                ViewBag.GetFileFormatText = new Func<FileFormat, string>(GetFileFormatText);

                return View(car);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading car details for ID: {CarId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin xe.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: CarOwner/Cars/Create
        public async Task<IActionResult> Create()
        {
            var car = new Car();
            await LoadDropdownsAsync();
            return View(car);
        }

        // POST: CarOwner/Cars/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
    [Bind("Name,Description,BrandId,CategoryId,Year,Color,LicensePlate,Seats,Transmission,FuelType,FuelConsumption,PricePerDay,PricePerHour,Location,Latitude,Longitude,MaxDeliveryDistance,PricePerKmDelivery")] Car model,
    IFormFile? primaryImage,
    List<IFormFile>? additionalImages,
    List<int>? selectedFeatureIds,
    int? model3DTemplateId,
    string? customModel3DUrl,
    int? model3DFileFormat)
        {
            _logger.LogInformation("=== CREATE CAR REQUEST ===");

            // GET USER & SET OwnerId
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogError("User not authenticated");
                return Challenge();
            }

            // Set properties
            model.OwnerId = user.Id;
            model.Status = CarStatus.PendingApproval;
            model.IsActive = true;
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;

            // ✅ REMOVE VALIDATION CHO NAVIGATION PROPERTIES
            ModelState.Remove("OwnerId");  // ← QUAN TRỌNG!
            ModelState.Remove("Owner");
            ModelState.Remove("Brand");
            ModelState.Remove("Category");
            ModelState.Remove("CarImages");
            ModelState.Remove("CarFeatures");
            ModelState.Remove("Rentals");
            ModelState.Remove("Reviews");
            ModelState.Remove("CarModel3D");

            _logger.LogInformation("Model after setting OwnerId: {@Model}", new
            {
                model.Name,
                model.BrandId,
                model.CategoryId,
                model.Year,
                model.PricePerDay,
                model.OwnerId,
                model.Status
            });

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("=== MODELSTATE INVALID ===");
                foreach (var modelState in ModelState)
                {
                    foreach (var error in modelState.Value.Errors)
                    {
                        _logger.LogWarning("Field: {Field}, Error: {Error}",
                            modelState.Key,
                            error.ErrorMessage ?? error.Exception?.Message ?? "Unknown");
                    }
                }

                ViewBag.ValidationErrors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value.Errors.Select(e => e.ErrorMessage ?? "Invalid value").ToList()
                    );

                await LoadDropdownsAsync();
                return View(model);
            }

            try
            {
                await _carService.CreateCarAsync(model);
                _logger.LogInformation("Car created successfully: {CarId}", model.CarId);

                // Upload images
                if (primaryImage != null)
                {
                    var primaryImageUrl = await _fileUploadService.UploadFileAsync(primaryImage, "cars");
                    await _carService.AddCarImageAsync(new CarImage
                    {
                        CarId = model.CarId,
                        ImageUrl = primaryImageUrl,
                        IsPrimary = true
                    });
                }

                if (additionalImages?.Any() == true)
                {
                    foreach (var image in additionalImages)
                    {
                        var imageUrl = await _fileUploadService.UploadFileAsync(image, "cars");
                        await _carService.AddCarImageAsync(new CarImage
                        {
                            CarId = model.CarId,
                            ImageUrl = imageUrl,
                            IsPrimary = false
                        });
                    }
                }

                // Add features
                if (selectedFeatureIds?.Any() == true)
                {
                    await _carService.AddCarFeaturesAsync(model.CarId, selectedFeatureIds);
                }

                // Add 3D model
                if (model3DTemplateId.HasValue || !string.IsNullOrEmpty(customModel3DUrl))
                {
                    var modelUrl = !string.IsNullOrEmpty(customModel3DUrl)
                        ? customModel3DUrl
                        : (await _model3DTemplateService.GetTemplateByIdAsync(model3DTemplateId.Value))?.ModelUrl;

                    if (!string.IsNullOrEmpty(modelUrl))
                    {
                        await _carService.AddCar3DModelAsync(new CarModel3D
                        {
                            CarId = model.CarId,
                            TemplateId = model3DTemplateId,
                            ModelUrl = modelUrl,
                            FileFormat = model3DFileFormat.HasValue ? (FileFormat?)model3DFileFormat.Value : null
                        });
                    }
                }

                TempData["SuccessMessage"] = "Xe đã được thêm thành công và đang chờ admin duyệt.";
                return RedirectToAction(nameof(Details), new { id = model.CarId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating car: {CarName}", model.Name);
                ModelState.AddModelError("", $"Có lỗi xảy ra khi thêm xe: {ex.Message}");

                if (ex.InnerException != null)
                {
                    ModelState.AddModelError("", $"Chi tiết: {ex.InnerException.Message}");
                }

                await LoadDropdownsAsync();
                return View(model);
            }
        }

        // GET: CarOwner/Cars/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var car = await _carService.GetCarWithDetailsAsync(id);

                if (car == null || car.OwnerId != user?.Id)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy xe hoặc bạn không có quyền chỉnh sửa xe này.";
                    return RedirectToAction(nameof(Index));
                }

                await LoadDropdownsAsync();

                // Pass existing data via ViewBag
                ViewBag.ExistingImages = car.CarImages?.OrderBy(img => !img.IsPrimary).ThenBy(img => img.CreatedAt).ToList();
                ViewBag.SelectedFeatureIds = car.CarFeatures?.Select(cf => cf.FeatureId).ToList() ?? new List<int>();

                if (car.CarModel3D != null)
                {
                    ViewBag.Current3DTemplateId = car.CarModel3D.TemplateId;
                    ViewBag.Current3DModelUrl = car.CarModel3D.ModelUrl;
                    ViewBag.Current3DFileFormat = car.CarModel3D.FileFormat;

                    if (car.CarModel3D.TemplateId.HasValue)
                    {
                        var template = await _model3DTemplateService.GetTemplateByIdAsync(car.CarModel3D.TemplateId.Value);
                        ViewBag.Current3DTemplateName = template?.Name;
                    }
                }

                _logger.LogInformation("Edit car {CarId}: Images: {ImageCount}, Features: {Features}",
                    id,
                    car.CarImages?.Count ?? 0,
                    string.Join(", ", ViewBag.SelectedFeatureIds as List<int> ?? new List<int>()));

                return View(car);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading car for edit: {CarId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin xe.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: CarOwner/Cars/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Car model,
            IFormFile? newPrimaryImage,
            List<IFormFile>? newAdditionalImages,
            List<int>? imagesToDelete,
            List<int>? selectedFeatureIds,
            int? model3DTemplateId,
            string? customModel3DUrl,
            int? model3DFileFormat,
            bool remove3DModel = false)
        {
            if (id != model.CarId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return View(model);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                var existingCar = await _carService.GetCarByIdAsync(id);

                if (existingCar == null || existingCar.OwnerId != user?.Id)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy xe hoặc bạn không có quyền chỉnh sửa xe này.";
                    return RedirectToAction(nameof(Index));
                }

                // Update car properties
                existingCar.Name = model.Name;
                existingCar.Description = model.Description;
                existingCar.BrandId = model.BrandId;
                existingCar.CategoryId = model.CategoryId;
                existingCar.Year = model.Year;
                existingCar.Color = model.Color;
                existingCar.LicensePlate = model.LicensePlate;
                existingCar.Seats = model.Seats;
                existingCar.Transmission = model.Transmission;
                existingCar.FuelType = model.FuelType;
                existingCar.FuelConsumption = model.FuelConsumption;
                existingCar.PricePerDay = model.PricePerDay;
                existingCar.PricePerHour = model.PricePerHour;
                existingCar.Location = model.Location;
                existingCar.Latitude = model.Latitude;
                existingCar.Longitude = model.Longitude;
                existingCar.UpdatedAt = DateTime.UtcNow;

                await _carService.UpdateCarAsync(existingCar);

                // Handle image deletion
                if (imagesToDelete?.Any() == true)
                {
                    foreach (var imageId in imagesToDelete)
                    {
                        var image = await _carService.GetCarImageByIdAsync(imageId);
                        if (image != null && image.CarId == id)
                        {
                            await _fileUploadService.DeleteFileAsync(image.ImageUrl);
                            await _carService.DeleteCarImageAsync(imageId);
                        }
                    }
                }

                // Upload new images
                if (newPrimaryImage != null)
                {
                    var imageUrl = await _fileUploadService.UploadFileAsync(newPrimaryImage, "cars");
                    await _carService.AddCarImageAsync(new CarImage
                    {
                        CarId = id,
                        ImageUrl = imageUrl,
                        IsPrimary = true
                    });
                }

                if (newAdditionalImages?.Any() == true)
                {
                    foreach (var image in newAdditionalImages)
                    {
                        var imageUrl = await _fileUploadService.UploadFileAsync(image, "cars");
                        await _carService.AddCarImageAsync(new CarImage
                        {
                            CarId = id,
                            ImageUrl = imageUrl,
                            IsPrimary = false
                        });
                    }
                }

                // Update features
                var currentFeatures = await _carService.GetCarFeaturesAsync(id);
                var currentFeatureIds = currentFeatures.Select(f => f.FeatureId).ToList();
                var newFeatureIds = selectedFeatureIds ?? new List<int>();

                var featuresToRemove = currentFeatureIds.Except(newFeatureIds);
                var featuresToAdd = newFeatureIds.Except(currentFeatureIds);

                if (featuresToRemove.Any())
                {
                    await _carService.RemoveCarFeaturesAsync(id, featuresToRemove);
                }

                if (featuresToAdd.Any())
                {
                    await _carService.AddCarFeaturesAsync(id, featuresToAdd);
                }

                // Handle 3D model
                if (remove3DModel)
                {
                    await _carService.RemoveCar3DModelAsync(id);
                }
                else if (model3DTemplateId.HasValue || !string.IsNullOrEmpty(customModel3DUrl))
                {
                    var existingModel3D = await _carService.GetCar3DModelAsync(id);
                    var modelUrl = !string.IsNullOrEmpty(customModel3DUrl)
                        ? customModel3DUrl
                        : (await _model3DTemplateService.GetTemplateByIdAsync(model3DTemplateId.Value))?.ModelUrl;

                    if (!string.IsNullOrEmpty(modelUrl))
                    {
                        if (existingModel3D != null)
                        {
                            existingModel3D.TemplateId = model3DTemplateId;
                            existingModel3D.ModelUrl = modelUrl;
                            existingModel3D.FileFormat = model3DFileFormat.HasValue ? (FileFormat?)model3DFileFormat.Value : null;
                            await _carService.UpdateCar3DModelAsync(existingModel3D);
                        }
                        else
                        {
                            await _carService.AddCar3DModelAsync(new CarModel3D
                            {
                                CarId = id,
                                TemplateId = model3DTemplateId,
                                ModelUrl = modelUrl,
                                FileFormat = model3DFileFormat.HasValue ? (FileFormat?)model3DFileFormat.Value : null
                            });
                        }
                    }
                }

                TempData["SuccessMessage"] = "Xe đã được cập nhật thành công.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating car: {CarId}", id);
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật xe.");
                await LoadDropdownsAsync();
                return View(model);
            }
        }

        // POST: CarOwner/Cars/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, CarStatus status)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var car = await _carService.GetCarByIdAsync(id);

                if (car == null || car.OwnerId != user?.Id)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy xe hoặc bạn không có quyền thay đổi trạng thái xe này.";
                    return RedirectToAction(nameof(Index));
                }

                if (status != CarStatus.Available && status != CarStatus.Maintenance)
                {
                    TempData["ErrorMessage"] = "Bạn chỉ có thể chuyển xe giữa trạng thái 'Sẵn sàng' và 'Bảo trì'.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                await _carService.UpdateCarStatusAsync(id, status);
                TempData["SuccessMessage"] = "Trạng thái xe đã được cập nhật thành công.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating car status: {CarId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật trạng thái xe.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: CarOwner/Cars/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var car = await _carService.GetCarWithDetailsAsync(id);

                if (car == null || car.OwnerId != user?.Id)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy xe hoặc bạn không có quyền xóa xe này.";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.GetCarStatusText = new Func<CarStatus, string>(GetCarStatusText);
                ViewBag.GetTransmissionText = new Func<Transmission?, string?>(GetTransmissionText);
                ViewBag.GetFuelTypeText = new Func<FuelType?, string?>(GetFuelTypeText);

                return View(car);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading car for delete: {CarId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin xe.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: CarOwner/Cars/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var car = await _carService.GetCarByIdAsync(id);

                if (car == null || car.OwnerId != user?.Id)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy xe hoặc bạn không có quyền xóa xe này.";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _carService.DeleteCarAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Xe đã được xóa thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể xóa xe.";
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
                _logger.LogError(ex, "Error deleting car: {CarId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa xe.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        private async Task LoadDropdownsAsync()
        {
            var brands = await _brandService.GetAllBrandsAsync();
            var categories = await _categoryService.GetAllCategoriesAsync();
            var features = await _featureService.GetAllFeaturesAsync();
            var templates3D = await _model3DTemplateService.GetActiveTemplatesAsync();

            ViewBag.Brands = new SelectList(brands, "BrandId", "Name");
            ViewBag.Categories = new SelectList(categories, "CategoryId", "Name");
            ViewBag.Features = features.ToList();
            ViewBag.Templates3D = templates3D.ToList();
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

        private string GetCarStatusText(CarStatus status)
        {
            return status switch
            {
                CarStatus.Available => "Sẵn sàng",
                CarStatus.Rented => "Đang thuê",
                CarStatus.Maintenance => "Bảo trì",
                CarStatus.PendingApproval => "Chờ duyệt",
                _ => "Không xác định"
            };
        }

        private string? GetTransmissionText(Transmission? transmission)
        {
            return transmission switch
            {
                Transmission.Manual => "Số sàn",
                Transmission.Automatic => "Số tự động",
                Transmission.CVT => "CVT",
                _ => null
            };
        }

        private string? GetFuelTypeText(FuelType? fuelType)
        {
            return fuelType switch
            {
                FuelType.Gasoline => "Xăng",
                FuelType.Diesel => "Dầu",
                FuelType.Hybrid => "Hybrid",
                FuelType.Electric => "Điện",
                _ => null
            };
        }
    }
}