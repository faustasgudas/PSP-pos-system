using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PsP.Contracts.GiftCards;
using PsP.Contracts.Common;
using PsP.Mappings;
using PsP.Services.Interfaces;

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

        // ========== GET OPERATIONS ==========

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(GiftCardResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GiftCardResponse>> GetById(int id)
        {
            var bizId = GetBusinessIdFromToken();

            _logger.LogInformation("Getting gift card by ID: {GiftCardId} for business {BusinessId}", id, bizId);

            var giftCard = await _giftCardService.GetByIdAsync(id);
            if (giftCard is null || giftCard.BusinessId != bizId)
            {
                _logger.LogWarning("Gift card {GiftCardId} not found or foreign for business {BusinessId}", id, bizId);
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

            _logger.LogInformation("Getting gift card by code: {Code} for business {BusinessId}", code, bizId);

            var giftCard = await _giftCardService.GetByCodeAsync(code);
            if (giftCard is null || giftCard.BusinessId != bizId)
            {
                _logger.LogWarning("Gift card with code {Code} not found or foreign for business {BusinessId}", code, bizId);
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

            _logger.LogInformation("Creating new gift card for business {BusinessId}", bizId);

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                var giftCard = request.ToNewEntity(); // mapperis su BusinessId iš request
                // PERRAŠOM BusinessId, kad būtų tik iš JWT
                giftCard.BusinessId = bizId;

                var created = await _giftCardService.CreateAsync(giftCard);

                _logger.LogInformation(
                    "Gift card created with ID: {GiftCardId} for business {BusinessId}",
                    created.GiftCardId, created.BusinessId);

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

            _logger.LogInformation("Updating balance for gift card {GiftCardId} by {Amount} (business {BusinessId})",
                id, request.Amount, bizId);

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            // Pirma pasižiūrim ar kortelė priklauso šitam business
            var card = await _giftCardService.GetByIdAsync(id);
            if (card is null || card.BusinessId != bizId)
            {
                _logger.LogWarning("Gift card {GiftCardId} not found or foreign for business {BusinessId}", id, bizId);
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

            _logger.LogInformation(
                "Redeeming {Amount} from gift card {GiftCardId} for business {BusinessId}",
                request.Amount, id, bizId);

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var card = await _giftCardService.GetByIdAsync(id);
            if (card is null || card.BusinessId != bizId)
            {
                _logger.LogWarning("Gift card {GiftCardId} not found or foreign for business {BusinessId}", id, bizId);
                return NotFound(new ApiErrorResponse("Gift card not found"));
            }

            try
            {
                var (charged, remaining) = await _giftCardService.RedeemAsync(id, request.Amount);

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

            _logger.LogInformation("Deactivating gift card {GiftCardId} for business {BusinessId}", id, bizId);

            var card = await _giftCardService.GetByIdAsync(id);
            if (card is null || card.BusinessId != bizId)
            {
                _logger.LogWarning("Gift card {GiftCardId} not found or foreign for business {BusinessId}", id, bizId);
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
