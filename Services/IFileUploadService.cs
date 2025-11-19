namespace carrentalmvc.Services
{
    public interface IFileUploadService
    {
        Task<string> UploadFileAsync(IFormFile file, string folder);
        Task<bool> DeleteFileAsync(string filePath);
        bool IsValidImageFile(IFormFile file);
        bool IsValidModelFile(IFormFile file);
        string GetFileExtension(IFormFile file);
    }
}