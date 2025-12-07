namespace PsP.Contracts.Discounts;

public class DiscountDetailResponse : DiscountSummaryResponse
{
    public List<DiscountEligibilityResponse> Eligibilities { get; set; } = new();
}