using System.Text.Json.Serialization;

namespace PsP.Models;
public class GiftCard
{
    public int GiftCardId { get; set; }

    public string Code { get; set; } = null!;
    public DateTime IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public long Balance { get; set; }
    public string Status { get; set; } = "Active";

    public int BusinessId { get; set; }
    
    public List<Payment> Payments { get; set; } = new();
}
