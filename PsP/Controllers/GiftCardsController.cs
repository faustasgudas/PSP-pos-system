using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PsP.Auth;
using PsP.Contracts.GiftCards;
using PsP.Contracts.Common;
using PsP.Mappings;
using PsP.Services.Interfaces;

namespace PsP.Controllers
{
    [ApiController]
    [Route("api/businesses/{businessId:int}/giftcards")]
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

        private ActionResult? EnsureBusinessMatchesRoute(int routeBusinessId)
        {
            var jwtBizId = User.GetBusinessId();
            if (jwtBizId != routeBusinessId)
            {
                _logger.LogWarning(
                    "Business mismatch: JWT={JwtBiz}, Route={RouteBiz}",
                    jwtBizId, routeBusinessId);
                return Forbid();
            }
            return null;
        }

       

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<GiftCardResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<GiftCardResponse>>> ListForBusiness(
            [FromRoute] int businessId,
            [FromQuery] string? status = null,
            [FromQuery] string? code = null)
        {
            var mismatch = EnsureBusinessMatchesRoute(businessId);
            if (mismatch is not null) return mismatch;

            var empId = User.GetEmployeeId();

            _logger.LogInformation(
                "Employee {EmployeeId} listing gift cards for business {BusinessId} (status={Status}, code={Code})",
                empId, businessId, status, code);

            var cards = await _giftCardService.GetByBusinessAsync(businessId, status, code);

            var resp = cards.Select(c => c.ToResponse());
            return Ok(resp);
        }

       

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(GiftCardResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GiftCardResponse>> GetById(
            [FromRoute] int businessId,
            [FromRoute] int id)
        {
            var mismatch = EnsureBusinessMatchesRoute(businessId);
            if (mismatch is not null) return mismatch;

            var empId = User.GetEmployeeId();

            _logger.LogInformation(
                "Employee {EmployeeId} getting gift card {GiftCardId} for business {BusinessId}",
                empId, id, businessId);

            var giftCard = await _giftCardService.GetByIdAsync(id);
            if (giftCard is null || giftCard.BusinessId != businessId)
            {
                _logger.LogWarning(
                    "Gift card {GiftCardId} not found or foreign for business {BusinessId}",
                    id, businessId);

                return NotFound(new ApiErrorResponse("Gift card not found"));
            }

            return Ok(giftCard.ToResponse());
        }

        [HttpGet("code/{code}")]
        [ProducesResponseType(typeof(GiftCardResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GiftCardResponse>> GetByCode(
            [FromRoute] int businessId,
            [FromRoute] string code)
        {
            var mismatch = EnsureBusinessMatchesRoute(businessId);
            if (mismatch is not null) return mismatch;

            var empId = User.GetEmployeeId();

            _logger.LogInformation(
                "Employee {EmployeeId} getting gift card by code {Code} for business {BusinessId}",
                empId, code, businessId);

            var giftCard = await _giftCardService.GetByCodeAsync(code);
            if (giftCard is null || giftCard.BusinessId != businessId)
            {
                _logger.LogWarning(
                    "Gift card with code {Code} not found or foreign for business {BusinessId}",
                    code, businessId);

                return NotFound(new ApiErrorResponse("Gift card not found"));
            }

            return Ok(giftCard.ToResponse());
        }

       

        [HttpPost]
        [ProducesResponseType(typeof(GiftCardResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<GiftCardResponse>> Create(
            [FromRoute] int businessId,
            [FromBody] CreateGiftCardRequest request)
        {
            var mismatch = EnsureBusinessMatchesRoute(businessId);
            if (mismatch is not null) return mismatch;

            var empId = User.GetEmployeeId();

            _logger.LogInformation(
                "Employee {EmployeeId} creating new gift card for business {BusinessId}",
                empId, businessId);

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                var giftCard = request.ToNewEntity();
                giftCard.BusinessId = businessId;

                var created = await _giftCardService.CreateAsync(giftCard);

                _logger.LogInformation(
                    "Gift card created with ID {GiftCardId} for business {BusinessId} by employee {EmployeeId}",
                    created.GiftCardId, created.BusinessId, empId);

                return CreatedAtAction(
                    nameof(GetById),
                    new { businessId, id = created.GiftCardId },
                    created.ToResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create gift card");
                return BadRequest(new ApiErrorResponse("Failed to create gift card", ex.Message));
            }
        }

        

        [HttpPatch("{id:int}/balance")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> UpdateBalance(
            [FromRoute] int businessId,
            [FromRoute] int id,
            [FromBody] UpdateBalanceRequest request)
        {
            var mismatch = EnsureBusinessMatchesRoute(businessId);
            if (mismatch is not null) return mismatch;

            var empId = User.GetEmployeeId();

            _logger.LogInformation(
                "Employee {EmployeeId} updating balance for gift card {GiftCardId} by {Amount} (business {BusinessId})",
                empId, id, request.Amount, businessId);

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var card = await _giftCardService.GetByIdAsync(id);
            if (card is null || card.BusinessId != businessId)
            {
                _logger.LogWarning(
                    "Gift card {GiftCardId} not found or foreign for business {BusinessId}",
                    id, businessId);

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

        

        [HttpPost("{id:int}/transactions")]
        [ProducesResponseType(typeof(RedeemResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<RedeemResponse>> Redeem(
            [FromRoute] int businessId,
            [FromRoute] int id,
            [FromBody] RedeemRequest request)
        {
            var mismatch = EnsureBusinessMatchesRoute(businessId);
            if (mismatch is not null) return mismatch;

            var empId = User.GetEmployeeId();

            _logger.LogInformation(
                "Employee {EmployeeId} redeeming {Amount} from gift card {GiftCardId} for business {BusinessId}",
                empId, request.Amount, id, businessId);

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var card = await _giftCardService.GetByIdAsync(id);
            if (card is null || card.BusinessId != businessId)
            {
                _logger.LogWarning(
                    "Gift card {GiftCardId} not found or foreign for business {BusinessId}",
                    id, businessId);

                return NotFound(new ApiErrorResponse("Gift card not found"));
            }

            try
            {
                var (charged, remaining) = await _giftCardService.RedeemAsync(id, request.Amount, businessId);

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
        public async Task<IActionResult> Deactivate(
            [FromRoute] int businessId,
            [FromRoute] int id)
        {
            var mismatch = EnsureBusinessMatchesRoute(businessId);
            if (mismatch is not null) return mismatch;

            var empId = User.GetEmployeeId();

            _logger.LogInformation(
                "Employee {EmployeeId} deactivating gift card {GiftCardId} for business {BusinessId}",
                empId, id, businessId);

            var card = await _giftCardService.GetByIdAsync(id);
            if (card is null || card.BusinessId != businessId)
            {
                _logger.LogWarning(
                    "Gift card {GiftCardId} not found or foreign for business {BusinessId}",
                    id, businessId);

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