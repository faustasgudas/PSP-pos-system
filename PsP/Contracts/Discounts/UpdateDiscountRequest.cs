namespace PsP.Contracts.Discounts;

public class UpdateDiscountRequest
{
    public string? Code { get; set; }
    public string? Type { get; set; }
    public string? Scope { get; set; }
    public decimal? Value { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public string? Status { get; set; }
}