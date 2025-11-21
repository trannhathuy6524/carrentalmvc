using carrentalmvc.Data.Constants;
using carrentalmvc.Models;
using carrentalmvc.Models.DTOs;
using carrentalmvc.Models.Enums;
using carrentalmvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace carrentalmvc.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtService jwtService,
            ILogger<AuthController> logger,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Đăng ký tài khoản mới
        /// POST: api/auth/register
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Dữ liệu không hợp lệ",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                if (request.Password != request.ConfirmPassword)
                {
                    return BadRequest(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Mật khẩu xác nhận không khớp"
                    });
                }

                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return BadRequest(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Email đã được sử dụng"
                    });
                }

                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FullName = request.FullName,
                    PhoneNumber = request.PhoneNumber,
                    DateOfBirth = request.DateOfBirth,
                    Address = request.Address,
                    UserType = UserType.Customer,
                    IsActive = true,
                    IsVerified = false, // Có thể thêm logic gửi email xác thực
                    EmailConfirmed = true, // Tạm thời set true cho mobile app
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, request.Password);

                if (!result.Succeeded)
                {
                    return BadRequest(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Đăng ký thất bại",
                        Errors = result.Errors.Select(e => e.Description)
                    });
                }

                // Gán role Customer mặc định
                await _userManager.AddToRoleAsync(user, RoleConstants.Customer);

                // Tạo JWT token
                var token = await _jwtService.GenerateJwtToken(user);
                var refreshToken = await _jwtService.GenerateRefreshToken();
                await _jwtService.SaveRefreshToken(user.Id, refreshToken);

                var expirationMinutes = int.Parse(_configuration["JwtSettings:ExpirationMinutes"] ?? "60");
                var roles = await _userManager.GetRolesAsync(user);

                _logger.LogInformation("User {Email} registered successfully", request.Email);

                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Đăng ký thành công",
                    Data = new AuthResponse
                    {
                        Token = token,
                        RefreshToken = refreshToken,
                        Expiration = DateTime.UtcNow.AddMinutes(expirationMinutes),
                        User = new UserDto
                        {
                            Id = user.Id,
                            Email = user.Email!,
                            FullName = user.FullName ?? string.Empty,
                            PhoneNumber = user.PhoneNumber,
                            Avatar = user.Avatar,
                            Roles = roles.ToList(),
                            IsVerified = user.IsVerified
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return StatusCode(500, new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra trong quá trình đăng ký"
                });
            }
        }

        /// <summary>
        /// Đăng nhập
        /// POST: api/auth/login
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Dữ liệu không hợp lệ",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return Unauthorized(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Email hoặc mật khẩu không đúng"
                    });
                }

                if (!user.IsActive)
                {
                    return Unauthorized(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Tài khoản đã bị vô hiệu hóa"
                    });
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);

                if (!result.Succeeded)
                {
                    return Unauthorized(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Email hoặc mật khẩu không đúng"
                    });
                }

                // Tạo JWT token
                var token = await _jwtService.GenerateJwtToken(user);
                var refreshToken = await _jwtService.GenerateRefreshToken();
                await _jwtService.SaveRefreshToken(user.Id, refreshToken);

                var expirationMinutes = int.Parse(_configuration["JwtSettings:ExpirationMinutes"] ?? "60");
                var roles = await _userManager.GetRolesAsync(user);

                _logger.LogInformation("User {Email} logged in successfully", request.Email);

                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Đăng nhập thành công",
                    Data = new AuthResponse
                    {
                        Token = token,
                        RefreshToken = refreshToken,
                        Expiration = DateTime.UtcNow.AddMinutes(expirationMinutes),
                        User = new UserDto
                        {
                            Id = user.Id,
                            Email = user.Email!,
                            FullName = user.FullName ?? string.Empty,
                            PhoneNumber = user.PhoneNumber,
                            Avatar = user.Avatar,
                            Roles = roles.ToList(),
                            IsVerified = user.IsVerified
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra trong quá trình đăng nhập"
                });
            }
        }

        /// <summary>
        /// Đăng xuất
        /// POST: api/auth/logout
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không tìm thấy thông tin người dùng"
                    });
                }

                // Revoke refresh token
                await _jwtService.RevokeRefreshToken(userId, request.RefreshToken);

                _logger.LogInformation("User {UserId} logged out successfully", userId);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Đăng xuất thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra trong quá trình đăng xuất"
                });
            }
        }

        /// <summary>
        /// Refresh token
        /// POST: api/auth/refresh-token
        /// </summary>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Token không hợp lệ"
                    });
                }

                var isValid = await _jwtService.ValidateRefreshToken(userId, request.RefreshToken);
                if (!isValid)
                {
                    return Unauthorized(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Refresh token không hợp lệ hoặc đã hết hạn"
                    });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null || !user.IsActive)
                {
                    return Unauthorized(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Tài khoản không tồn tại hoặc đã bị vô hiệu hóa"
                    });
                }

                // Revoke old refresh token và tạo mới
                await _jwtService.RevokeRefreshToken(userId, request.RefreshToken);
                
                var newToken = await _jwtService.GenerateJwtToken(user);
                var newRefreshToken = await _jwtService.GenerateRefreshToken();
                await _jwtService.SaveRefreshToken(user.Id, newRefreshToken);

                var expirationMinutes = int.Parse(_configuration["JwtSettings:ExpirationMinutes"] ?? "60");
                var roles = await _userManager.GetRolesAsync(user);

                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Refresh token thành công",
                    Data = new AuthResponse
                    {
                        Token = newToken,
                        RefreshToken = newRefreshToken,
                        Expiration = DateTime.UtcNow.AddMinutes(expirationMinutes),
                        User = new UserDto
                        {
                            Id = user.Id,
                            Email = user.Email!,
                            FullName = user.FullName ?? string.Empty,
                            PhoneNumber = user.PhoneNumber,
                            Avatar = user.Avatar,
                            Roles = roles.ToList(),
                            IsVerified = user.IsVerified
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during refresh token");
                return StatusCode(500, new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra trong quá trình refresh token"
                });
            }
        }

        /// <summary>
        /// Lấy thông tin user hiện tại
        /// GET: api/auth/me
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy thông tin người dùng"
                    });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "Người dùng không tồn tại"
                    });
                }

                var roles = await _userManager.GetRolesAsync(user);

                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = "Lấy thông tin thành công",
                    Data = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email!,
                        FullName = user.FullName ?? string.Empty,
                        PhoneNumber = user.PhoneNumber,
                        Avatar = user.Avatar,
                        Roles = roles.ToList(),
                        IsVerified = user.IsVerified
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra"
                });
            }
        }
    }
}