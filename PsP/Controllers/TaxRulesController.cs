using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PsP.Auth;
using PsP.Contracts.Tax;
using PsP.Data;
using PsP.Models;

namespace PsP.Controllers;

[ApiController]
[Route("api/tax-rules")]
public class TaxRulesController : ControllerBase
{
    private readonly AppDbContext _db;
    public TaxRulesController(AppDbContext db) => _db = db;

 
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? countryCode = null, [FromQuery] string? taxClass = null)
    {
       
        var q = _db.TaxRules.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(countryCode)) q = q.Where(x => x.CountryCode == countryCode);
        if (!string.IsNullOrWhiteSpace(taxClass)) q = q.Where(x => x.TaxClass == taxClass);

        var list = await q.OrderByDescending(x => x.ValidFrom).ToListAsync();
        return Ok(list);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaxRuleRequest body)
    {

        if (string.IsNullOrWhiteSpace(body.CountryCode) || body.CountryCode.Length != 2)
            return BadRequest("country_code_invalid");
        if (string.IsNullOrWhiteSpace(body.TaxClass) || body.TaxClass.Length > 32)
            return BadRequest("tax_class_invalid");
       
        if (body.ValidTo <= body.ValidFrom)
            return BadRequest("valid_window_invalid");


        var overlaps = await _db.TaxRules.AnyAsync(x =>
            x.CountryCode == body.CountryCode &&
            x.TaxClass == body.TaxClass &&
            x.ValidFrom < body.ValidTo &&
            x.ValidTo > body.ValidFrom);

        if (overlaps)
            return Conflict("tax_rule_overlaps_existing");

        var entity = new TaxRule
        {
            CountryCode = body.CountryCode,
            TaxClass = body.TaxClass,
            RatePercent = body.RatePercent,
            ValidFrom = body.ValidFrom,
            ValidTo = body.ValidTo
        };

        _db.TaxRules.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(List), new { countryCode = entity.CountryCode, taxClass = entity.TaxClass }, entity);
    }
}
