using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PsP.Models;
using PsP.Contracts.Businesses;
using PsP.Contracts.Common;
using PsP.Services.Interfaces;

namespace PsP.Controllers;

[ApiController]
[Route("api/businesses")]
public class BusinessesController : ControllerBase
{
    private readonly IBusinessService _businessService;
    private readonly ILogger<BusinessesController> _logger;

    public BusinessesController(
        IBusinessService businessService,
        ILogger<BusinessesController> logger)
    {
        _businessService = businessService;
        _logger = logger;
    }

    // ========== GET: /api/businesses ==========

    [HttpGet]
    [ProducesResponseType(typeof(List<BusinessResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BusinessResponse>>> GetAll()
    {
        _logger.LogInformation("Getting all businesses");

        var businesses = await _businessService.GetAllAsync();
        var response = businesses
            .Select(BusinessResponse.FromEntity)
            .ToList();

        return Ok(response);
    }

    // ========== GET: /api/businesses/{id} ==========

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BusinessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BusinessResponse>> GetById(int id)
    {
        _logger.LogInformation("Getting business by ID: {BusinessId}", id);

        var business = await _businessService.GetByIdAsync(id);
        if (business is null)
        {
            _logger.LogWarning("Business {BusinessId} not found", id);
            return NotFound(new ApiErrorResponse("Business not found"));
        }

        return Ok(BusinessResponse.FromEntity(business));
    }

    // ========== POST: /api/businesses ==========

    [HttpPost]
    [ProducesResponseType(typeof(BusinessResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BusinessResponse>> Create([FromBody] CreateBusinessRequest request)
    {
        _logger.LogInformation("Creating new business");

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var entity = new Business
            {
                Name = request.Name,
                Address = request.Address,
                Phone = request.Phone,
                Email = request.Email,
                CountryCode = request.CountryCode,
                PriceIncludesTax = request.PriceIncludesTax,
                BusinessStatus = "Active"
            };

            var created = await _businessService.CreateAsync(entity);

            _logger.LogInformation("Business created with ID: {BusinessId}", created.BusinessId);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.BusinessId },
                BusinessResponse.FromEntity(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create business");
            return BadRequest(new ApiErrorResponse("Failed to create business", ex.Message));
        }
    }

    // ========== PUT: /api/businesses/{id} ==========

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(BusinessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BusinessResponse>> Update(
        int id,
        [FromBody] UpdateBusinessRequest request)
    {
        _logger.LogInformation("Updating business {BusinessId}", id);

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var updatedEntity = new Business
            {
                BusinessId = id, // nebūtina, bet ok
                Name = request.Name,
                Address = request.Address,
                Phone = request.Phone,
                Email = request.Email,
                CountryCode = request.CountryCode,
                PriceIncludesTax = request.PriceIncludesTax
            };

            var updated = await _businessService.UpdateAsync(id, updatedEntity);
            if (updated is null)
            {
                _logger.LogWarning("Business {BusinessId} not found for update", id);
                return NotFound(new ApiErrorResponse("Business not found"));
            }

            return Ok(BusinessResponse.FromEntity(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update business {BusinessId}", id);
            return BadRequest(new ApiErrorResponse("Failed to update business", ex.Message));
        }
    }

    // ========== DELETE: /api/businesses/{id} ==========

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        _logger.LogInformation("Deleting business {BusinessId}", id);

        var success = await _businessService.DeleteAsync(id);
        if (!success)
        {
            _logger.LogWarning("Business {BusinessId} not found for delete", id);
            return NotFound(new ApiErrorResponse("Business not found"));
        }

        _logger.LogInformation("Business {BusinessId} deleted", id);
        return NoContent();
    }
}
