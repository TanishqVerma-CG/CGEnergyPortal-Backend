using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BP.IService;
using BP.Models;
using Microsoft.IdentityModel.Tokens;

namespace BP.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public LoginResponse GenerateToken(User user, string role)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expiryMinutes = int.Parse(jwtSettings["ExpiryInMinutes"] ?? "15");

            var expiry = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,   user.EmailId),
                new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.EmailId),
                new Claim("UserId", user.UserId.ToString()),
                new Claim("Name", user.Name),
                new Claim(ClaimTypes.Name, user.EmailId),
                new Claim(ClaimTypes.Role, role)
            };

            var signingCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiry,
                signingCredentials: signingCredentials);

            return new LoginResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiry = expiry,
                Username = user.Name,
                UserId = user.UserId,
                Role = role
            };
        }
    }
}
