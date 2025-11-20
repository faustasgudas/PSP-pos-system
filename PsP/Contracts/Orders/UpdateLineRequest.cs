namespace PsP.Contracts.Orders;

public class UpdateLineRequest
{
    public decimal? Qty { get; set; }
    public int? DiscountId { get; set; }
}