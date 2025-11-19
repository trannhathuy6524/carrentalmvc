using carrentalmvc.Models;
using carrentalmvc.Repositories;

namespace carrentalmvc.Services
{
    public class FeatureService : IFeatureService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<FeatureService> _logger;

        public FeatureService(IUnitOfWork unitOfWork, ILogger<FeatureService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IEnumerable<Feature>> GetAllFeaturesAsync()
        {
            return await _unitOfWork.Features.GetAllAsync();
        }

        public async Task<IEnumerable<Feature>> GetActiveFeaturesAsync()
        {
            return await _unitOfWork.Features.GetActiveeFeaturesAsync();
        }

        public async Task<Feature?> GetFeatureByIdAsync(int id)
        {
            return await _unitOfWork.Features.GetByIdAsync(id);
        }

        public async Task<Feature?> GetFeatureByNameAsync(string name)
        {
            return await _unitOfWork.Features.GetByNameAsync(name);
        }

        public async Task<Feature> CreateFeatureAsync(Feature feature)
        {
            try
            {
                if (await FeatureNameExistsAsync(feature.Name))
                {
                    throw new InvalidOperationException($"Feature với tên '{feature.Name}' đã tồn tại.");
                }

                feature.CreatedAt = DateTime.UtcNow;
                feature.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Features.AddAsync(feature);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Đã tạo feature mới: {FeatureName} (ID: {FeatureId})", feature.Name, feature.FeatureId);
                return feature;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo feature: {FeatureName}", feature.Name);
                throw;
            }
        }

        public async Task<Feature> UpdateFeatureAsync(Feature feature)
        {
            try
            {
                var existingFeature = await _unitOfWork.Features.GetByIdAsync(feature.FeatureId);
                if (existingFeature == null)
                {
                    throw new InvalidOperationException($"Không tìm thấy feature với ID: {feature.FeatureId}");
                }

                if (await FeatureNameExistsAsync(feature.Name, feature.FeatureId))
                {
                    throw new InvalidOperationException($"Feature với tên '{feature.Name}' đã tồn tại.");
                }

                existingFeature.Name = feature.Name;
                existingFeature.Description = feature.Description;
                existingFeature.Icon = feature.Icon;
                existingFeature.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Features.Update(existingFeature);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Đã cập nhật feature: {FeatureName} (ID: {FeatureId})", feature.Name, feature.FeatureId);
                return existingFeature;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật feature ID: {FeatureId}", feature.FeatureId);
                throw;
            }
        }

        public async Task<bool> DeleteFeatureAsync(int id)
        {
            try
            {
                var feature = await _unitOfWork.Features.GetByIdAsync(id);
                if (feature == null)
                {
                    return false;
                }

                _unitOfWork.Features.Remove(feature);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Đã xóa feature: {FeatureName} (ID: {FeatureId})", feature.Name, id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa feature ID: {FeatureId}", id);
                throw;
            }
        }

        public async Task<bool> FeatureExistsAsync(int id)
        {
            return await _unitOfWork.Features.ExistsAsync(id);
        }

        public async Task<bool> FeatureNameExistsAsync(string name, int? excludeId = null)
        {
            var existingFeature = await _unitOfWork.Features.GetByNameAsync(name);
            if (existingFeature == null)
            {
                return false;
            }

            return excludeId == null || existingFeature.FeatureId != excludeId;
        }

        public async Task<IEnumerable<Feature>> GetCarFeaturesAsync(int carId)
        {
            return await _unitOfWork.Features.GetCarFeaturesAsync(carId);
        }
    }
}