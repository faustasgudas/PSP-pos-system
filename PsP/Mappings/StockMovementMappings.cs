using PsP.Contracts.StockMovements;
using PsP.Models;

namespace PsP.Mappings;

public static class StockMovementMappings
{
        public static StockMovementResponse ToResponse(this StockMovement m) => new()
    {
        StockMovementId   = m.StockMovementId,
        StockItemId       = m.StockItemId,
        Type              = m.Type,
        Delta             = m.Delta,
        UnitCostSnapshot  = m.UnitCostSnapshot,
        OrderLineId       = m.OrderLineId,
        At                = m.At
        
    };

  
    public static StockMovement ToNewEntity(this CreateStockMovementRequest req, int stockItemId, DateTime? nowUtc = null)
    {
        if (string.IsNullOrWhiteSpace(req.Type)) throw new ArgumentException("Type required");

        var type = NormalizeType(req.Type);
        var delta = req.Delta;

        
        if (type == "Sale" && delta > 0) delta = -delta;
        if (type == "RefundReturn" && delta < 0) delta = -delta;

        return new StockMovement
        {
            StockItemId      = stockItemId,
            OrderLineId      = req.OrderLineId,
            Type             = type,
            Delta            = delta,
            UnitCostSnapshot = req.UnitCostSnapshot,
            At               = req.At ?? nowUtc ?? DateTime.UtcNow
        };
    }

    private static string NormalizeType(string type)
    {
        var t = type.Trim();
        return t.Equals("receive",        StringComparison.OrdinalIgnoreCase) ? "Receive"       :
               t.Equals("sale",           StringComparison.OrdinalIgnoreCase) ? "Sale"          :
               t.Equals("refundreturn",   StringComparison.OrdinalIgnoreCase) ? "RefundReturn"  :
               t.Equals("waste",          StringComparison.OrdinalIgnoreCase) ? "Waste"         :
               t.Equals("adjust",         StringComparison.OrdinalIgnoreCase) ? "Adjust"        :
               t;
    }
}