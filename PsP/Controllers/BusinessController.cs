using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PsP.Contracts.Businesses;
using PsP.Contracts.Common;
using PsP.Mappings;
using PsP.Services.Interfaces;

namespace PsP.Controllers;

[ApiController]
[Route("api/businesses")]
[Authorize] 
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

    private int GetBusinessIdFromToken()
    {
        var claim = User.FindFirst("businessId")
                    ?? throw new InvalidOperationException("Missing businessId claim");
        return int.Parse(claim.Value);
    }

    // ========== GET: /api/businesses ==========
    // Grąžinam TIK prisijungusio employee business (ne visą sąrašą)

    [HttpGet]
    [ProducesResponseType(typeof(List<BusinessResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BusinessResponse>>> GetAll()
    {
        var bizId = GetBusinessIdFromToken();

        _logger.LogInformation("Getting business for current user. BusinessId from token: {BusinessId}", bizId);

        var business = await _businessService.GetByIdAsync(bizId);
        if (business is null)
        {
            _logger.LogWarning("Business {BusinessId} (from token) not found", bizId);
            return NotFound(new ApiErrorResponse("Business not found"));
        }

        var response = new List<BusinessResponse> { business.ToResponse() };
        return Ok(response);
    }

    // ========== GET: /api/businesses/{id} ==========

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BusinessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BusinessResponse>> GetById(int id)
    {
        var tokenBizId = GetBusinessIdFromToken();
        if (tokenBizId != id)
        {
            _logger.LogWarning("Attempt to access foreign business. TokenBizId={TokenBizId}, RouteId={RouteId}",
                tokenBizId, id);
            return Forbid();
        }

        _logger.LogInformation("Getting business by ID: {BusinessId}", id);

        var business = await _businessService.GetByIdAsync(id);
        if (business is null)
        {
            _logger.LogWarning("Business {BusinessId} not found", id);
            return NotFound(new ApiErrorResponse("Business not found"));
        }

        return Ok(business.ToResponse());
    }

    // ========== POST: /api/businesses ==========
    // Šitą dažniausiai pakeičia /api/auth/register-business,
    // bet paliekam, jei norėsi kurti biz'us iš vidaus (pvz. admin)

    [HttpPost]
    [ProducesResponseType(typeof(BusinessResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BusinessResponse>> Create([FromBody] CreateBusinessRequest request)
    {
        _logger.LogInformation("Creating new business (internal endpoint)");

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var entity = request.ToNewEntity();

            var created = await _businessService.CreateAsync(entity);

            _logger.LogInformation("Business created with ID: {BusinessId}", created.BusinessId);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.BusinessId },
                created.ToResponse());
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
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BusinessResponse>> Update(
        int id,
        [FromBody] UpdateBusinessRequest request)
    {
        var tokenBizId = GetBusinessIdFromToken();
        if (tokenBizId != id)
        {
            _logger.LogWarning("Attempt to update foreign business. TokenBizId={TokenBizId}, RouteId={RouteId}",
                tokenBizId, id);
            return Forbid();
        }

        _logger.LogInformation("Updating business {BusinessId}", id);

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var updatedEntity = request.ToUpdatedEntity(id);

            var updated = await _businessService.UpdateAsync(id, updatedEntity);
            if (updated is null)
            {
                _logger.LogWarning("Business {BusinessId} not found for update", id);
                return NotFound(new ApiErrorResponse("Business not found"));
            }

            return Ok(updated.ToResponse());
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
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id)
    {
        var tokenBizId = GetBusinessIdFromToken();
        if (tokenBizId != id)
        {
            _logger.LogWarning("Attempt to delete foreign business. TokenBizId={TokenBizId}, RouteId={RouteId}",
                tokenBizId, id);
            return Forbid();
        }

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
