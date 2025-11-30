namespace PsP.Contracts.Discounts;

public class DiscountSummaryResponse
{
    public int DiscountId { get; set; }
    public int BusinessId { get; set; }
    public string Code { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Scope { get; set; } = null!;
    public decimal Value { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string Status { get; set; } = null!;
}