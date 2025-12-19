using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PsP.Contracts.Auth;
using PsP.Data;
using PsP.Models;
using PsP.Services.Implementations.Auth;
using PsP.Services.Interfaces.Auth;
using BCrypt.Net;

namespace PsP.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(AppDbContext db, IJwtTokenService jwtTokenService)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
    }

    // ========= REGISTER BUSINESS + OWNER =========

    [AllowAnonymous]
    [HttpPost("register-business")]
    public async Task<ActionResult<RegisterBusinessResponse>> RegisterBusiness(
        [FromBody] RegisterBusinessRequest req)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var email = req.OwnerEmail.Trim().ToLowerInvariant();
        var exists = await _db.Employees.AnyAsync(e => e.Email.ToLower() == email);
        if (exists)
            return Conflict(new { error = "owner_email_in_use" });

        var biz = new Business
        {
            Name             = req.BusinessName.Trim(),
            Address          = req.Address.Trim(),
            Phone            = req.Phone.Trim(),
            Email            = req.Email.Trim(),
            CountryCode      = req.CountryCode.Trim().ToUpperInvariant(),
            PriceIncludesTax = req.PriceIncludesTax,
            BusinessStatus   = "Active",
            BusinessType     = string.IsNullOrWhiteSpace(req.BusinessType)
                ? "Catering"
                : req.BusinessType.Trim()
        };

        _db.Businesses.Add(biz);
        await _db.SaveChangesAsync();

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

        var token = _jwtTokenService.GenerateToken(biz, owner);

        var resp = new RegisterBusinessResponse
        {
            BusinessId      = biz.BusinessId,
            OwnerEmployeeId = owner.EmployeeId,
            Token           = token,
            BusinessType    = biz.BusinessType
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
            .Include(e => e.Business)
            .FirstOrDefaultAsync(e => e.Email.ToLower() == normalizedEmail);

        if (emp is null)
            return Unauthorized(new { error = "invalid_credentials" });

        if (!BCrypt.Net.BCrypt.Verify(req.Password, emp.PasswordHash))
            return Unauthorized(new { error = "invalid_credentials" });

        if (emp.Status != "Active")
            return Unauthorized(new { error = "inactive_employee" });

        if (emp.Business is null)
            return Unauthorized(new { error = "no_business_linked" });

        var token = _jwtTokenService.GenerateToken(emp.Business, emp);

        return Ok(new LoginResponse
        {
            Token        = token,
            BusinessType = emp.Business.BusinessType  // 👈 čia frontend ui selector’iui
        });
    }

}
