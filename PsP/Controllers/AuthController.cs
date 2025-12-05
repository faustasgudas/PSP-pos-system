using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PsP.Contracts.Auth;
using PsP.Data;
using PsP.Models;
using PsP.Settings;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PsP.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtSettings _jwt;

    public AuthController(AppDbContext db, IOptions<JwtSettings> jwt)
    {
        _db = db;
        _jwt = jwt.Value;
    }

    // ========= REGISTER BUSINESS + OWNER =========

    [AllowAnonymous]
    [HttpPost("register-business")]
    public async Task<ActionResult<RegisterBusinessResponse>> RegisterBusiness([FromBody] RegisterBusinessRequest req)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        // 1) patikrinam ar owner email jau nenaudojamas
        var email = req.OwnerEmail.Trim().ToLowerInvariant();
        var exists = await _db.Employees.AnyAsync(e => e.Email.ToLower() == email);
        if (exists)
            return Conflict(new { error = "owner_email_in_use" });

        // 2) sukuriam Business
        var biz = new Business
        {
            Name             = req.BusinessName.Trim(),
            Address          = req.Address.Trim(),
            Phone            = req.Phone.Trim(),
            Email            = req.Email.Trim(),
            CountryCode      = req.CountryCode.Trim().ToUpperInvariant(),
            PriceIncludesTax = req.PriceIncludesTax,
            BusinessStatus   = "Active"
        };

        _db.Businesses.Add(biz);
        await _db.SaveChangesAsync();

        // 3) sukuriam Owner employee
        var owner = new Employee
        {
            BusinessId   = biz.BusinessId,
            Name         = req.OwnerName.Trim(),
            Email        = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.OwnerPassword),
            Role         = "Owner",
            Status       = "Active"
        };

        _db.Employees.Add(owner);
        await _db.SaveChangesAsync();

        // 4) sugeneruojam JWT
        var token = GenerateToken(owner);

        var resp = new RegisterBusinessResponse
        {
            BusinessId      = biz.BusinessId,
            OwnerEmployeeId = owner.EmployeeId,
            Token           = token
        };

        return Ok(resp);
    }

    // ========= LOGIN =========

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var normalizedEmail = req.Email.Trim().ToLowerInvariant();

        var emp = await _db.Employees
            .FirstOrDefaultAsync(e => e.Email.ToLower() == normalizedEmail);

        if (emp is null)
            return Unauthorized(new { error = "invalid_credentials" });

        if (!BCrypt.Net.BCrypt.Verify(req.Password, emp.PasswordHash))
            return Unauthorized(new { error = "invalid_credentials" });

        var token = GenerateToken(emp);

        return Ok(new LoginResponse { Token = token });
    }

    // ========= JWT GENERATION =========

    private string GenerateToken(Employee emp)
    {
        // 🔐 Saugumo / konfigo validacija
        if (string.IsNullOrWhiteSpace(_jwt.Key))
            throw new InvalidOperationException("JWT Key is missing. Please configure Jwt:Key in appsettings.");

        if (string.IsNullOrWhiteSpace(_jwt.Issuer))
            throw new InvalidOperationException("JWT Issuer is missing. Please configure Jwt:Issuer in appsettings.");

        if (string.IsNullOrWhiteSpace(_jwt.Audience))
            throw new InvalidOperationException("JWT Audience is missing. Please configure Jwt:Audience in appsettings.");

        if (_jwt.ExpiresMinutes <= 0)
            throw new InvalidOperationException("Jwt:ExpiresMinutes must be a positive number.");

        var keyBytes = Encoding.UTF8.GetBytes(_jwt.Key);
        var key      = new SymmetricSecurityKey(keyBytes);
        var creds    = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            // Custom claims
            new Claim("employeeId", emp.EmployeeId.ToString()),
            new Claim("businessId", emp.BusinessId.ToString()),
            new Claim(ClaimTypes.Role, emp.Role),
            new Claim(ClaimTypes.Name, emp.Name ?? string.Empty),

            // JWT "standartiniai" claim'ai
            new Claim(JwtRegisteredClaimNames.Sub, emp.EmployeeId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer:            _jwt.Issuer,
            audience:          _jwt.Audience,
            claims:            claims,
            notBefore:         DateTime.UtcNow,
            expires:           DateTime.UtcNow.AddMinutes(_jwt.ExpiresMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
