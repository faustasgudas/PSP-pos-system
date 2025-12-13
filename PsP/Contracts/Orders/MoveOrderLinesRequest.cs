namespace PsP.Contracts.Orders;

public class MoveOrderLinesRequest
{
    public int TargetOrderId { get; set; }
    public List<MoveOrderLineRequest> Lines { get; set; } = new();
}