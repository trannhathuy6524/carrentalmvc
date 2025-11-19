using carrentalmvc.Data;
using carrentalmvc.Models;
using carrentalmvc.Repositories;
using carrentalmvc.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

// Đăng ký SeedService
builder.Services.AddScoped<SeedService>();

// Đăng ký Repositories
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IBrandRepository, BrandRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICarRepository, CarRepository>();
builder.Services.AddScoped<IFeatureRepository, FeatureRepository>();
builder.Services.AddScoped<IRentalRepository, RentalRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentDistributionRepository, PaymentDistributionRepository>();

// Đăng ký Services
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICarService, CarService>();
builder.Services.AddScoped<IFeatureService, FeatureService>();
builder.Services.AddScoped<IRentalService, RentalService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentDistributionService, PaymentDistributionService>();
// Add services
builder.Services.AddScoped<IModel3DTemplateService, Model3DTemplateService>();
builder.Services.AddScoped<IModel3DTemplateRepository, Model3DTemplateRepository>();
// Add file upload service
builder.Services.AddScoped<IFileUploadService, FileUploadService>();

builder.Services.AddHttpClient();
var app = builder.Build();

// Create upload directories
var webRootPath = app.Environment.WebRootPath;
var uploadPaths = new[]
{
    Path.Combine(webRootPath, "uploads"),
    Path.Combine(webRootPath, "uploads", "models"),
    Path.Combine(webRootPath, "uploads", "previews")
};

foreach (var path in uploadPaths)
{
    if (!Directory.Exists(path))
    {
        Directory.CreateDirectory(path);
        Console.WriteLine($"Created directory: {path}");
    }
}

// Seed database
using (var scope = app.Services.CreateScope())
{
    try
    {
        var seedService = scope.ServiceProvider.GetRequiredService<SeedService>();
        await seedService.SeedAllAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

// Enable serving static files from wwwroot
app.UseStaticFiles();

// QUAN TRỌNG: Serve files từ uploads folder
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.WebRootPath, "uploads")),
    RequestPath = "/uploads",
    ServeUnknownFileTypes = true, // Allow .glb files
    DefaultContentType = "application/octet-stream"
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "carowner_area",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}",
    constraints: new { area = "CarOwner" });

app.MapControllerRoute(
    name: "customer_area",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}",
    constraints: new { area = "Customer" });

app.MapControllerRoute(
    name: "admin_area",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}",
    constraints: new { area = "Admin" });

app.MapControllerRoute(
    name: "driver_area",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}",
    constraints: new { area = "Driver" });

app.MapControllerRoute(
    name: "staff_area",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}",
    constraints: new { area = "Staff" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();