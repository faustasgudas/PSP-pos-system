using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PsP.Contracts.GiftCards;
using PsP.Contracts.Common;
using PsP.Mappings;
using PsP.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PsP.Controllers
{
    [ApiController]
    [Route("api/giftcards")]
    [Authorize]
    public class GiftCardsController : ControllerBase
    {
        private readonly IGiftCardService _giftCardService;
        private readonly ILogger<GiftCardsController> _logger;

        public GiftCardsController(
            IGiftCardService giftCardService,
            ILogger<GiftCardsController> logger)
        {
            _giftCardService = giftCardService;
            _logger = logger;
        }

        private int GetBusinessIdFromToken()
        {
            var claim = User.FindFirst("businessId")
                        ?? throw new InvalidOperationException("Missing businessId claim");
            return int.Parse(claim.Value);
        }

        private int GetEmployeeIdFromToken()
        {
            var claim = User.FindFirst("employeeId")
                        ?? throw new InvalidOperationException("Missing employeeId claim");
            return int.Parse(claim.Value);
        }

        // ========== LIST ==========

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<GiftCardResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<GiftCardResponse>>> ListForBusiness(
            [FromQuery] string? status = null,
            [FromQuery] string? code = null)
        {
            var bizId = GetBusinessIdFromToken();
            var empId = GetEmployeeIdFromToken();

            _logger.LogInformation(
                "Employee {EmployeeId} listing gift cards for business {BusinessId} (status={Status}, code={Code})",
                empId, bizId, status, code);

            var cards = await _giftCardService.GetByBusinessAsync(bizId, status, code);

            var resp = cards.Select(c => c.ToResponse());
            return Ok(resp);
        }

        // ========== GET OPERATIONS ==========

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(GiftCardResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GiftCardResponse>> GetById(int id)
        {
            var bizId = GetBusinessIdFromToken();
            var empId = GetEmployeeIdFromToken();

            _logger.LogInformation(
                "Employee {EmployeeId} getting gift card {GiftCardId} for business {BusinessId}",
                empId, id, bizId);

            var giftCard = await _giftCardService.GetByIdAsync(id);
            if (giftCard is null || giftCard.BusinessId != bizId)
            {
                _logger.LogWarning(
                    "Gift card {GiftCardId} not found or foreign for business {BusinessId}",
                    id, bizId);

                return NotFound(new ApiErrorResponse("Gift card not found"));
            }

            return Ok(giftCard.ToResponse());
        }

        [HttpGet("code/{code}")]
        [ProducesResponseType(typeof(GiftCardResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GiftCardResponse>> GetByCode(string code)
        {
            var bizId = GetBusinessIdFromToken();
            var empId = GetEmployeeIdFromToken();

            _logger.LogInformation(
                "Employee {EmployeeId} getting gift card by code {Code} for business {BusinessId}",
                empId, code, bizId);

            var giftCard = await _giftCardService.GetByCodeAsync(code);
            if (giftCard is null || giftCard.BusinessId != bizId)
            {
                _logger.LogWarning(
                    "Gift card with code {Code} not found or foreign for business {BusinessId}",
                    code, bizId);

                return NotFound(new ApiErrorResponse("Gift card not found"));
            }

            return Ok(giftCard.ToResponse());
        }

        // ========== CREATE OPERATIONS ==========

        [HttpPost]
        [ProducesResponseType(typeof(GiftCardResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<GiftCardResponse>> Create([FromBody] CreateGiftCardRequest request)
        {
            var bizId = GetBusinessIdFromToken();
            var empId = GetEmployeeIdFromToken();

            _logger.LogInformation(
                "Employee {EmployeeId} creating new gift card for business {BusinessId}",
                empId, bizId);

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                var giftCard = request.ToNewEntity();
                // BusinessId visada iš JWT
                giftCard.BusinessId = bizId;

                var created = await _giftCardService.CreateAsync(giftCard);

                _logger.LogInformation(
                    "Gift card created with ID {GiftCardId} for business {BusinessId} by employee {EmployeeId}",
                    created.GiftCardId, created.BusinessId, empId);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = created.GiftCardId },
                    created.ToResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create gift card");
                return BadRequest(new ApiErrorResponse("Failed to create gift card", ex.Message));
            }
        }

        // ========== UPDATE OPERATIONS ==========

        [HttpPatch("{id:int}/balance")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> UpdateBalance(int id, [FromBody] UpdateBalanceRequest request)
        {
            var bizId = GetBusinessIdFromToken();
            var empId = GetEmployeeIdFromToken();

            _logger.LogInformation(
                "Employee {EmployeeId} updating balance for gift card {GiftCardId} by {Amount} (business {BusinessId})",
                empId, id, request.Amount, bizId);

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var card = await _giftCardService.GetByIdAsync(id);
            if (card is null || card.BusinessId != bizId)
            {
                _logger.LogWarning(
                    "Gift card {GiftCardId} not found or foreign for business {BusinessId}",
                    id, bizId);

                return NotFound(new ApiErrorResponse("Gift card not found"));
            }

            try
            {
                var success = await _giftCardService.TopUpAsync(id, request.Amount);
                if (!success)
                {
                    _logger.LogWarning("Gift card {GiftCardId} not found for balance update", id);
                    return NotFound(new ApiErrorResponse("Gift card not found"));
                }

                _logger.LogInformation("Balance updated for gift card {GiftCardId}", id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business rule violation for gift card {GiftCardId}", id);
                return UnprocessableEntity(new ApiErrorResponse("Operation failed", ex.Message));
            }
        }

        // ========== BUSINESS OPERATIONS ==========

        [HttpPost("{id:int}/transactions")]
        [ProducesResponseType(typeof(RedeemResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<RedeemResponse>> Redeem(int id, [FromBody] RedeemRequest request)
        {
            var bizId = GetBusinessIdFromToken();
            var empId = GetEmployeeIdFromToken();

            _logger.LogInformation(
                "Employee {EmployeeId} redeeming {Amount} from gift card {GiftCardId} for business {BusinessId}",
                empId, request.Amount, id, bizId);

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var card = await _giftCardService.GetByIdAsync(id);
            if (card is null || card.BusinessId != bizId)
            {
                _logger.LogWarning(
                    "Gift card {GiftCardId} not found or foreign for business {BusinessId}",
                    id, bizId);

                return NotFound(new ApiErrorResponse("Gift card not found"));
            }

            try
            {
                var (charged, remaining) = await _giftCardService.RedeemAsync(id, request.Amount, bizId);

                _logger.LogInformation(
                    "Redeemed {Charged} from gift card {GiftCardId}, remaining: {Remaining}",
                    charged, id, remaining);

                return Ok(new RedeemResponse(charged, remaining));
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning("Gift card {GiftCardId} not found for redeem", id);
                return NotFound(new ApiErrorResponse("Gift card not found"));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business rule violation during redeem for gift card {GiftCardId}", id);
                return UnprocessableEntity(new ApiErrorResponse("Redeem failed", ex.Message));
            }
        }

        [HttpPost("{id:int}/deactivate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Deactivate(int id)
        {
            var bizId = GetBusinessIdFromToken();
            var empId = GetEmployeeIdFromToken();

            _logger.LogInformation(
                "Employee {EmployeeId} deactivating gift card {GiftCardId} for business {BusinessId}",
                empId, id, bizId);

            var card = await _giftCardService.GetByIdAsync(id);
            if (card is null || card.BusinessId != bizId)
            {
                _logger.LogWarning(
                    "Gift card {GiftCardId} not found or foreign for business {BusinessId}",
                    id, bizId);

                return NotFound(new ApiErrorResponse("Gift card not found"));
            }

            var success = await _giftCardService.DeactivateAsync(id);
            if (!success)
            {
                _logger.LogWarning("Gift card {GiftCardId} not found for deactivation", id);
                return NotFound(new ApiErrorResponse("Gift card not found"));
            }

            _logger.LogInformation("Gift card {GiftCardId} deactivated", id);
            return NoContent();
        }
    }
}
