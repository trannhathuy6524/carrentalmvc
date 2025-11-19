using carrentalmvc.Models;
using carrentalmvc.Repositories;

namespace carrentalmvc.Services
{
    public class Model3DTemplateService : IModel3DTemplateService
    {
        private readonly IModel3DTemplateRepository _templateRepository;
        private readonly ICarRepository _carRepository;
        private readonly IUnitOfWork _unitOfWork;

        public Model3DTemplateService(
            IModel3DTemplateRepository templateRepository,
            ICarRepository carRepository,
            IUnitOfWork unitOfWork)
        {
            _templateRepository = templateRepository;
            _carRepository = carRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Model3DTemplate>> GetAllTemplatesAsync()
        {
            return await _templateRepository.GetAllWithIncludesAsync();
        }

        public async Task<IEnumerable<Model3DTemplate>> GetActiveTemplatesAsync()
        {
            return await _templateRepository.GetActiveTemplatesAsync();
        }

        public async Task<IEnumerable<Model3DTemplate>> GetTemplatesByBrandAsync(int brandId)
        {
            return await _templateRepository.GetTemplatesByBrandAsync(brandId);
        }

        public async Task<IEnumerable<Model3DTemplate>> GetTemplatesByCategoryAsync(int categoryId)
        {
            return await _templateRepository.GetTemplatesByCategoryAsync(categoryId);
        }

        public async Task<Model3DTemplate?> GetTemplateByIdAsync(int id)
        {
            return await _templateRepository.GetByIdWithIncludesAsync(id);
        }

        public async Task<Model3DTemplate> CreateTemplateAsync(Model3DTemplate template)
        {
            // Validate unique name
            var existingTemplate = await _templateRepository.GetByNameAsync(template.Name);
            if (existingTemplate != null)
            {
                throw new InvalidOperationException("Đã tồn tại mô hình 3D với tên này.");
            }

            template.CreatedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;

            await _templateRepository.AddAsync(template);
            await _unitOfWork.SaveAsync();

            return template;
        }

        public async Task<Model3DTemplate> UpdateTemplateAsync(Model3DTemplate template)
        {
            var existingTemplate = await _templateRepository.GetByIdAsync(template.TemplateId);
            if (existingTemplate == null)
            {
                throw new InvalidOperationException("Không tìm thấy mô hình 3D.");
            }

            // Check for duplicate name (exclude current template)
            var duplicateTemplate = await _templateRepository.GetByNameAsync(template.Name);
            if (duplicateTemplate != null && duplicateTemplate.TemplateId != template.TemplateId)
            {
                throw new InvalidOperationException("Đã tồn tại mô hình 3D với tên này.");
            }

            existingTemplate.Name = template.Name;
            existingTemplate.Description = template.Description;
            existingTemplate.ModelUrl = template.ModelUrl;
            existingTemplate.PreviewImageUrl = template.PreviewImageUrl;
            existingTemplate.FileFormat = template.FileFormat;
            existingTemplate.FileSize = template.FileSize;
            existingTemplate.BrandId = template.BrandId;
            existingTemplate.CategoryId = template.CategoryId;
            existingTemplate.IsActive = template.IsActive;
            existingTemplate.UpdatedAt = DateTime.UtcNow;

            _templateRepository.Update(existingTemplate);
            await _unitOfWork.SaveAsync();

            return existingTemplate;
        }

        public async Task<bool> DeleteTemplateAsync(int id)
        {
            var template = await _templateRepository.GetByIdAsync(id);
            if (template == null)
            {
                return false;
            }

            // Check if template is being used by any cars
            var usageCount = await GetUsageCountAsync(id);
            if (usageCount > 0)
            {
                throw new InvalidOperationException($"Không thể xóa mô hình 3D này vì đang được sử dụng bởi {usageCount} xe.");
            }

            _templateRepository.Remove(template);
            await _unitOfWork.SaveAsync();

            return true;
        }

        public async Task<bool> ToggleActiveStatusAsync(int id)
        {
            var template = await _templateRepository.GetByIdAsync(id);
            if (template == null)
            {
                return false;
            }

            template.IsActive = !template.IsActive;
            template.UpdatedAt = DateTime.UtcNow;

            _templateRepository.Update(template);
            await _unitOfWork.SaveAsync();

            return true;
        }

        public async Task<int> GetUsageCountAsync(int templateId)
        {
            return await _templateRepository.GetUsageCountAsync(templateId);
        }
    }
}