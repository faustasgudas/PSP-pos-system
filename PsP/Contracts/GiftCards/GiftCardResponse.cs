using PsP.Models;

namespace PsP.Contracts.GiftCards;

public class GiftCardResponse
{
    public int GiftCardId { get; set; }
    public string Code { get; set; } = string.Empty;
    public long Balance { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public DateTime IssuedAt { get; set; }
}
