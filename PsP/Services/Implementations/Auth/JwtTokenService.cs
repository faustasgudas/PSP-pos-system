using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PsP.Models;
using PsP.Services.Interfaces.Auth;
using PsP.Settings;

namespace PsP.Services.Implementations.Auth;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public string GenerateToken(Business business, Employee employee)
    {
        if (string.IsNullOrWhiteSpace(_settings.Key) ||
            string.IsNullOrWhiteSpace(_settings.Issuer) ||
            string.IsNullOrWhiteSpace(_settings.Audience))
        {
            throw new InvalidOperationException("JWT settings are not configured properly.");
        }

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, employee.EmployeeId.ToString()),
            new(JwtRegisteredClaimNames.Email, employee.Email ?? business.Email),
            new("businessId", business.BusinessId.ToString()),
            new("employeeId", employee.EmployeeId.ToString()),
            new(ClaimTypes.Role, employee.Role),
            new("status", employee.Status)
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpiresMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}