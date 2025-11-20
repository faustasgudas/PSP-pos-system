namespace PsP.Models;

public class Discount
{
    public int DiscountId { get; set; }

    public string Code { get; set; } = null!;          // pvz. "HAPPYHOUR"
    public string Type { get; set; } = null!;          // "Percent" | "Amount"
    public string Scope { get; set; } = null!;         // "Order" | "Line"
    public decimal Value { get; set; }                 // 10 (%, ar pinigai – pagal Type)

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }

    public string Status { get; set; } = "Active";     // "Active" | "Inactive"
    
    public int BusinessId { get; set; }
    public Business Business { get; set; } = null!;

    public List<DiscountEligibility> Eligibilities { get; set; } = new();
}