using carrentalmvc.Services;

namespace carrentalmvc.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileUploadService> _logger;

        private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private readonly string[] _allowedModelExtensions = { ".glb", ".gltf", ".obj", ".fbx" };
        private readonly long _maxImageSize = 5 * 1024 * 1024; // 5MB
        private readonly long _maxModelSize = 100 * 1024 * 1024; // 100MB

        public FileUploadService(IWebHostEnvironment environment, ILogger<FileUploadService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folder)
        {
            try
            {
                if (file == null || file.Length == 0)
                    throw new ArgumentException("File không hợp lệ");

                // Create upload directory if not exists
                var uploadDir = Path.Combine(_environment.WebRootPath, "uploads", folder);
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                    _logger.LogInformation("Created directory: {Directory}", uploadDir);
                }

                // Generate unique filename
                var extension = Path.GetExtension(file.FileName).ToLower();
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadDir, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Log successful upload
                _logger.LogInformation("File uploaded successfully: {FilePath}, Size: {FileSize}", filePath, file.Length);

                // Return relative URL
                var relativeUrl = $"/uploads/{folder}/{fileName}";
                _logger.LogInformation("Returning URL: {Url}", relativeUrl);

                return relativeUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file: {FileName}", file?.FileName);
                throw new InvalidOperationException("Không thể tải lên file. Vui lòng thử lại.");
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !filePath.StartsWith("/uploads/"))
                    return false;

                var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
                return false;
            }
        }

        public bool IsValidImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            if (file.Length > _maxImageSize)
                return false;

            var extension = Path.GetExtension(file.FileName).ToLower();
            return _allowedImageExtensions.Contains(extension);
        }

        public bool IsValidModelFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            if (file.Length > _maxModelSize)
                return false;

            var extension = Path.GetExtension(file.FileName).ToLower();
            return _allowedModelExtensions.Contains(extension);
        }

        public string GetFileExtension(IFormFile file)
        {
            return Path.GetExtension(file.FileName).ToLower();
        }
    }
}