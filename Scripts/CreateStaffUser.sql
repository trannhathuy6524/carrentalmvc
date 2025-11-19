-- Script to manually create Staff role and user

-- 1. Check if Staff role exists
SELECT * FROM AspNetRoles WHERE Name = 'Staff';

-- 2. If not exists, create Staff role
IF NOT EXISTS (SELECT * FROM AspNetRoles WHERE Name = 'Staff')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'Staff', 'STAFF', NEWID());
    PRINT 'Staff role created';
END
ELSE
BEGIN
    PRINT 'Staff role already exists';
END

-- 3. Check if staff user exists
SELECT u.Id, u.Email, u.FullName, r.Name AS Role
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Email = 'staff@carrentalmvc.com';

-- 4. If user doesn't exist, you need to:
-- OPTION A: Use application to register (recommended)
-- OPTION B: Run seeding from Program.cs

-- 5. To add Staff role to existing user:
-- DECLARE @userId NVARCHAR(450);
-- DECLARE @roleId NVARCHAR(450);
-- 
-- SELECT @userId = Id FROM AspNetUsers WHERE Email = 'staff@carrentalmvc.com';
-- SELECT @roleId = Id FROM AspNetRoles WHERE Name = 'Staff';
-- 
-- IF @userId IS NOT NULL AND @roleId IS NOT NULL
-- BEGIN
--     IF NOT EXISTS (SELECT * FROM AspNetUserRoles WHERE UserId = @userId AND RoleId = @roleId)
--     BEGIN
--         INSERT INTO AspNetUserRoles (UserId, RoleId)
--         VALUES (@userId, @roleId);
--         PRINT 'Staff role assigned to user';
--     END
--     ELSE
--     BEGIN
--         PRINT 'User already has Staff role';
--     END
-- END
