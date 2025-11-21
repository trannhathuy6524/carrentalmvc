using carrentalmvc.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace carrentalmvc.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<JwtService> _logger;
        
        // In-memory storage cho refresh tokens (production nên dùng database hoặc Redis)
        private static readonly Dictionary<string, HashSet<string>> _refreshTokens = new();

        public JwtService(
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            ILogger<JwtService> logger)
        {
            _configuration = configuration;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("fullName", user.FullName ?? string.Empty),
                new Claim("isVerified", user.IsVerified.ToString())
            };

            // Thêm roles vào claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured")));
            
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            
            var expirationMinutes = int.Parse(_configuration["JwtSettings:ExpirationMinutes"] ?? "60");
            var expiration = DateTime.UtcNow.AddMinutes(expirationMinutes);

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: expiration,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public Task<string> GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Task.FromResult(Convert.ToBase64String(randomNumber));
        }

        public Task<bool> ValidateRefreshToken(string userId, string refreshToken)
        {
            if (!_refreshTokens.ContainsKey(userId))
                return Task.FromResult(false);

            return Task.FromResult(_refreshTokens[userId].Contains(refreshToken));
        }

        public Task SaveRefreshToken(string userId, string refreshToken)
        {
            if (!_refreshTokens.ContainsKey(userId))
            {
                _refreshTokens[userId] = new HashSet<string>();
            }

            _refreshTokens[userId].Add(refreshToken);
            return Task.CompletedTask;
        }

        public Task RevokeRefreshToken(string userId, string refreshToken)
        {
            if (_refreshTokens.ContainsKey(userId))
            {
                _refreshTokens[userId].Remove(refreshToken);
            }

            return Task.CompletedTask;
        }
    }
}