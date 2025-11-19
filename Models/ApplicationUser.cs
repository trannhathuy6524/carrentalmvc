using carrentalmvc.Models.Enums;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace carrentalmvc.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Avatar { get; set; }

        public UserType UserType { get; set; } = UserType.Customer;

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public bool IsVerified { get; set; } = false; // Xác thực tài khoản

        public string? NationalId { get; set; } // CMND/CCCD
        public string? DriverLicense { get; set; } // GPLX

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<Car> Cars { get; set; } = new List<Car>();
        public virtual ICollection<Rental> Rentals { get; set; } = new List<Rental>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        // ✅ THÊM các properties sau:

        [InverseProperty("CarOwner")]
        public virtual ICollection<DriverAssignment> ManagedDrivers { get; set; } = new List<DriverAssignment>();

        [InverseProperty("Driver")]
        public virtual ICollection<DriverAssignment> DriverAssignments { get; set; } = new List<DriverAssignment>();

        [InverseProperty("Driver")]
        public virtual ICollection<Rental> DriverRentals { get; set; } = new List<Rental>();
    }
}