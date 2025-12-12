namespace PsP.Contracts.Orders;

public class MoveOrderLineRequest
{
    public int OrderLineId { get; set; }
    public decimal Qty { get; set; }
}