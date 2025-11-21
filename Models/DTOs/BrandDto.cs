namespace carrentalmvc.Models.DTOs
{
    public class BrandDto
    {
        public int BrandId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public int CarCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class BrandDetailDto : BrandDto
    {
        public List<CarBriefDto>? Cars { get; set; }
    }

    public class CarBriefDto
    {
        public int CarId { get; set; }
        public string Model { get; set; } = string.Empty;
        public decimal PricePerDay { get; set; }
        public string? ImageUrl { get; set; }
        public double? Rating { get; set; }
        public int ReviewCount { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public object? Errors { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}