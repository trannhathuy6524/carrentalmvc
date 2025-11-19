using carrentalmvc.Data.Constants;
using carrentalmvc.Models;
using carrentalmvc.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace carrentalmvc.Data
{
    public class SeedService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<SeedService> _logger;

        public SeedService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<SeedService> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task SeedAllAsync()
        {
            try
            {
                await _context.Database.EnsureCreatedAsync();

                await SeedRolesAsync();
                await SeedUsersAsync();
                await SeedBrandsAsync();
                await SeedCategoriesAsync();
                await SeedFeaturesAsync();

                _logger.LogInformation("Database seeding completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }

        private async Task SeedRolesAsync()
        {
            _logger.LogInformation("Seeding roles...");

            foreach (var roleName in RoleConstants.AllRoles)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    var role = new IdentityRole(roleName);
                    var result = await _roleManager.CreateAsync(role);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation($"Created role: {roleName}");
                    }
                    else
                    {
                        _logger.LogError($"Failed to create role {roleName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
        }

        private async Task SeedUsersAsync()
        {
            _logger.LogInformation("Seeding users...");

            // Admin user
            await CreateUserIfNotExistsAsync(
                email: "admin@carrentalmvc.com",
                password: "Admin@123456",
                fullName: "Quản trị viên hệ thống",
                userType: UserType.Admin,
                role: RoleConstants.Admin,
                phoneNumber: "0900000000"
            );

            // Staff user
            await CreateUserIfNotExistsAsync(
                email: "staff@carrentalmvc.com",
                password: "Staff@123456",
                fullName: "Nhân viên hỗ trợ",
                userType: UserType.Admin, // Dùng UserType.Admin cho Staff
                role: RoleConstants.Staff,
                phoneNumber: "0900000001"
            );

            // Sample customers
            await CreateUserIfNotExistsAsync(
                email: "customer1@example.com",
                password: "Customer@123",
                fullName: "Nguyễn Văn An",
                userType: UserType.Customer,
                role: RoleConstants.Customer,
                phoneNumber: "0901111111",
                address: "123 Nguyễn Trãi, Quận 1, TP.HCM",
                nationalId: "123456789001",
                driverLicense: "B1-12345678",
                dateOfBirth: new DateTime(1990, 1, 15)
            );

            await CreateUserIfNotExistsAsync(
                email: "customer2@example.com",
                password: "Customer@123",
                fullName: "Trần Thị Bình",
                userType: UserType.Customer,
                role: RoleConstants.Customer,
                phoneNumber: "0902222222",
                address: "456 Lê Lợi, Quận 3, TP.HCM",
                nationalId: "123456789002",
                driverLicense: "B1-87654321",
                dateOfBirth: new DateTime(1992, 5, 20)
            );

            // Sample car owners
            await CreateUserIfNotExistsAsync(
                email: "owner1@example.com",
                password: "Owner@123",
                fullName: "Lê Văn Chủ",
                userType: UserType.Owner,
                role: RoleConstants.Owner,
                phoneNumber: "0903333333",
                address: "789 Điện Biên Phủ, Quận Bình Thạnh, TP.HCM",
                nationalId: "123456789003",
                driverLicense: "B2-11223344",
                dateOfBirth: new DateTime(1985, 3, 10)
            );

            await CreateUserIfNotExistsAsync(
                email: "owner2@example.com",
                password: "Owner@123",
                fullName: "Phạm Thị Dung",
                userType: UserType.Owner,
                role: RoleConstants.Owner,
                phoneNumber: "0904444444",
                address: "321 Võ Văn Tần, Quận 10, TP.HCM",
                nationalId: "123456789004",
                driverLicense: "B2-44332211",
                dateOfBirth: new DateTime(1988, 7, 25)
            );
        }

        private async Task CreateUserIfNotExistsAsync(
            string email,
            string password,
            string fullName,
            UserType userType,
            string role,
            string? phoneNumber = null,
            string? address = null,
            string? nationalId = null,
            string? driverLicense = null,
            DateTime? dateOfBirth = null)
        {
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                return;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                UserType = userType,
                IsActive = true,
                IsVerified = true,
                PhoneNumber = phoneNumber,
                Address = address,
                NationalId = nationalId,
                DriverLicense = driverLicense,
                DateOfBirth = dateOfBirth,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);
                _logger.LogInformation($"Created user: {email} with role: {role}");
            }
            else
            {
                _logger.LogError($"Failed to create user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        private async Task SeedBrandsAsync()
        {
            if (await _context.Brands.AnyAsync())
                return;

            _logger.LogInformation("Seeding brands...");

            var brands = new[]
            {
                new Brand { Name = "Toyota", Description = "Thương hiệu xe Nhật Bản nổi tiếng", LogoUrl = "/images/brands/toyota.png", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Brand { Name = "Honda", Description = "Thương hiệu xe Nhật Bản tin cậy", LogoUrl = "/images/brands/honda.png", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Brand { Name = "Hyundai", Description = "Thương hiệu xe Hàn Quốc hiện đại", LogoUrl = "/images/brands/hyundai.png", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Brand { Name = "Ford", Description = "Thương hiệu xe Mỹ mạnh mẽ", LogoUrl = "/images/brands/ford.png", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Brand { Name = "Mazda", Description = "Thương hiệu xe Nhật Bản thể thao", LogoUrl = "/images/brands/mazda.png", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Brand { Name = "Kia", Description = "Thương hiệu xe Hàn Quốc trẻ trung", LogoUrl = "/images/brands/kia.png", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Brand { Name = "BMW", Description = "Thương hiệu xe Đức sang trọng", LogoUrl = "/images/brands/bmw.png", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Brand { Name = "Mercedes-Benz", Description = "Thương hiệu xe Đức đẳng cấp", LogoUrl = "/images/brands/mercedes.png", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Brand { Name = "Audi", Description = "Thương hiệu xe Đức công nghệ cao", LogoUrl = "/images/brands/audi.png", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Brand { Name = "Vinfast", Description = "Thương hiệu xe Việt Nam", LogoUrl = "/images/brands/vinfast.png", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            };

            _context.Brands.AddRange(brands);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Seeded {brands.Length} brands.");
        }

        private async Task SeedCategoriesAsync()
        {
            if (await _context.Categories.AnyAsync())
                return;

            _logger.LogInformation("Seeding categories...");

            var categories = new[]
            {
                new Category { Name = "Sedan", Description = "Xe sedan 4-5 chỗ ngồi, phù hợp di chuyển trong thành phố", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { Name = "SUV", Description = "Xe SUV đa dụng, thích hợp cho gia đình và địa hình khó", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { Name = "Hatchback", Description = "Xe hatchback nhỏ gọn, tiết kiệm nhiên liệu", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { Name = "MPV", Description = "Xe đa dụng gia đình, rộng rãi thoải mái", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { Name = "Pickup", Description = "Xe bán tải, phù hợp vận chuyển hàng hóa", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { Name = "Coupe", Description = "Xe coupe thể thao 2 cửa", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { Name = "Convertible", Description = "Xe mui trần sang trọng", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { Name = "Luxury", Description = "Xe sang cao cấp, đẳng cấp thượng lưu", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            };

            _context.Categories.AddRange(categories);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Seeded {categories.Length} categories.");
        }

        private async Task SeedFeaturesAsync()
        {
            if (await _context.Features.AnyAsync())
                return;

            _logger.LogInformation("Seeding features...");

            var features = new[]
            {
                new Feature { Name = "Điều hòa", Description = "Hệ thống điều hòa không khí tự động", Icon = "fas fa-snowflake", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Feature { Name = "GPS", Description = "Hệ thống định vị và dẫn đường GPS", Icon = "fas fa-map-marker-alt", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Feature { Name = "Bluetooth", Description = "Kết nối Bluetooth không dây", Icon = "fab fa-bluetooth", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Feature { Name = "Camera lùi", Description = "Camera quan sát hỗ trợ lùi xe", Icon = "fas fa-video", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Feature { Name = "Cửa sổ trời", Description = "Cửa sổ trời panorama toàn cảnh", Icon = "fas fa-sun", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Feature { Name = "Ghế da", Description = "Ghế bọc da cao cấp", Icon = "fas fa-chair", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Feature { Name = "Cảm biến lùi", Description = "Cảm biến hỗ trợ đỗ xe an toàn", Icon = "fas fa-satellite-dish", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Feature { Name = "USB", Description = "Cổng kết nối USB sạc điện thoại", Icon = "fab fa-usb", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Feature { Name = "Wifi hotspot", Description = "Phát wifi trong xe", Icon = "fas fa-wifi", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Feature { Name = "Màn hình cảm ứng", Description = "Màn hình giải trí cảm ứng", Icon = "fas fa-tablet-alt", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Feature { Name = "Cruise Control", Description = "Hệ thống ga tự động", Icon = "fas fa-tachometer-alt", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Feature { Name = "Phanh ABS", Description = "Hệ thống phanh chống bó cứng", Icon = "fas fa-shield-alt", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Feature { Name = "Túi khí", Description = "Hệ thống túi khí an toàn", Icon = "fas fa-life-ring", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Feature { Name = "Khởi động bằng nút", Description = "Khởi động xe bằng nút bấm", Icon = "fas fa-power-off", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Feature { Name = "Đèn LED", Description = "Hệ thống đèn LED tiết kiệm năng lượng", Icon = "fas fa-lightbulb", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            };

            _context.Features.AddRange(features);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Seeded {features.Length} features.");
        }
    }
}