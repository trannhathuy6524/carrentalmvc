namespace carrentalmvc.Data.Constants
{
    public static class RoleConstants
    {
        public const string Admin = "Admin";
        public const string Staff = "Staff"; // ✅ THÊM ROLE STAFF
        public const string Customer = "Customer";
        public const string Owner = "Owner";
        public const string Driver = "Driver";

        public static readonly string[] AllRoles = { Admin, Staff, Customer, Owner, Driver };
    }
}