namespace PsP.Contracts.Orders;

public class OrderDetailResponse
{
    public int OrderId { get; set; }
    public int BusinessId { get; set; }
    public int EmployeeId { get; set; }
    public int? ReservationId { get; set; }
    public string Status { get; set; } = "";
    public string? TableOrArea { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal TipAmount { get; set; }
    public int? DiscountId { get; set; }
    public string? OrderDiscountSnapshot { get; set; }
    public List<OrderLineResponse> Lines { get; set; } = new();
}