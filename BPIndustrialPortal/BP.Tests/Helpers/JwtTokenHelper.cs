using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace BP.Tests.Helpers;

/// <summary>
/// Generates JWT tokens signed with the test-only key configured by BrokerApiFactory.
/// </summary>
internal static class JwtTokenHelper
{
    internal const string Key = "test-only-signing-key-not-for-deployment-2026";
    private const string Issuer = "BP";
    private const string Audience = "BP";

    public static string GenerateBrokerToken() => GenerateToken("Broker");

    public static string GenerateBillingToken() => GenerateToken("Billing");

    private static string GenerateToken(string role)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user"),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
