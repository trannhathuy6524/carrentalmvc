using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using carrentalmvc.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace carrentalmvc.Models
{
    public class Car
    {
        public int CarId { get; set; }

        [Required(ErrorMessage = "Tên xe là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên xe không được vượt quá 200 ký tự")]
        public string Name { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Năm sản xuất là bắt buộc")]
        [Range(1900, 2100, ErrorMessage = "Năm sản xuất phải từ 1900 đến 2100")]
        public int Year { get; set; }

        [Required(ErrorMessage = "Giá thuê theo ngày là bắt buộc")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999, ErrorMessage = "Giá thuê phải lớn hơn 0")]
        public decimal PricePerDay { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999, ErrorMessage = "Giá thuê theo giờ phải lớn hơn 0")]
        public decimal? PricePerHour { get; set; }

        public CarStatus Status { get; set; } = CarStatus.Available;

        [StringLength(50)]
        public string? Color { get; set; }

        [StringLength(20)]
        public string? LicensePlate { get; set; }

        [Range(1, 100, ErrorMessage = "Số chỗ ngồi phải từ 1 đến 100")]
        public int? Seats { get; set; }

        public FuelType? FuelType { get; set; }
        public Transmission? Transmission { get; set; }

        [Range(0, 100, ErrorMessage = "Mức tiêu hao nhiên liệu phải từ 0 đến 100 L/100km")]
        public double? FuelConsumption { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        public string? Location { get; set; }

        [Range(-90, 90, ErrorMessage = "Vĩ độ phải từ -90 đến 90")]
        public double? Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Kinh độ phải từ -180 đến 180")]
        public double? Longitude { get; set; }

        [Range(0, 500, ErrorMessage = "Khoảng cách giao xe phải từ 0 đến 500 km")]
        public int? MaxDeliveryDistance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 100000, ErrorMessage = "Giá giao xe phải từ 0 đến 100,000 VNĐ/km")]
        public decimal? PricePerKmDelivery { get; set; }

        // ✅ THÊM [ValidateNever] CHO OwnerId
        [ValidateNever]
        public string OwnerId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Thương hiệu là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn thương hiệu")]
        public int BrandId { get; set; }

        [Required(ErrorMessage = "Loại xe là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn loại xe")]
        public int CategoryId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ✅ Navigation Properties - THÊM [ValidateNever]
        [ValidateNever]
        public virtual ApplicationUser Owner { get; set; } = null!;

        [ValidateNever]
        public virtual Brand Brand { get; set; } = null!;

        [ValidateNever]
        public virtual Category Category { get; set; } = null!;

        [ValidateNever]
        public virtual ICollection<CarImage> CarImages { get; set; } = new List<CarImage>();

        [ValidateNever]
        public virtual ICollection<CarFeature> CarFeatures { get; set; } = new List<CarFeature>();

        [ValidateNever]
        public virtual ICollection<Rental> Rentals { get; set; } = new List<Rental>();

        [ValidateNever]
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

        [ValidateNever]
        public virtual CarModel3D? CarModel3D { get; set; }
    }
}