using PsP.Models;
using PsP.Services;
using Microsoft.AspNetCore.Mvc;

namespace PsP.Controllers
{
    [ApiController]
    [Route("api/giftcards")]
    public class GiftCardsController : ControllerBase
    {
        private readonly IGiftCardService _svc;
        public GiftCardsController(IGiftCardService svc) => _svc = svc;

        [HttpPost]
        public async Task<ActionResult<GiftCard>> Create([FromBody] GiftCard gc)
        {
            var created = await _svc.CreateAsync(gc);
            return CreatedAtAction(nameof(GetById), new { id = created.GiftCardId }, created);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<GiftCard>> GetById(int id)
        {
            var card = await _svc.GetByIdAsync(id); // <-- kviečiam servisą
            return card is null ? NotFound() : Ok(card);
        }

        // jei nori likti prie raw string body – palik šitą
        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] string code)
        {
            try
            {
                var c = await _svc.ValidateAsync(code);
                return c is null ? NotFound(new { error = "not_found" })
                                 : Ok(new { c.Balance, c.Status, c.ExpiresAt, id = c.GiftCardId });
            }
            catch (InvalidOperationException ex)
            {
                return UnprocessableEntity(new { error = ex.Message });
            }
        }

        [HttpPost("{id:int}/topup")]
        public async Task<IActionResult> TopUp(int id, [FromBody] decimal amount)
        {
            try
            {
                var ok = await _svc.TopUpAsync(id, amount);
                return ok ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                return UnprocessableEntity(new { error = ex.Message });
            }
        }

        [HttpPost("redeem")]
        public async Task<IActionResult> Redeem([FromBody] RedeemReq req)
        {
            try
            {
                var (charged, remaining) = await _svc.RedeemAsync(req.Code, req.Amount);
                return Ok(new { charged, remaining });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "not_found" });
            }
            catch (InvalidOperationException ex)
            {
                return UnprocessableEntity(new { error = ex.Message });
            }
        }

        public record RedeemReq(string Code, decimal Amount);
    }
}
