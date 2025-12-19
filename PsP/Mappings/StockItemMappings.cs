using PsP.Contracts.StockItems;
using PsP.Models;

namespace PsP.Mappings;

public static class StockItemMappings
{
    public static StockItemSummaryResponse ToSummaryResponse(this StockItem s) => new()
    {
        StockItemId  = s.StockItemId,
        CatalogItemId= s.CatalogItemId,
        Unit         = s.Unit,
        QtyOnHand    = s.QtyOnHand
    };

    public static StockItemDetailResponse ToDetailResponse(this StockItem s) => new()
    {
        StockItemId      = s.StockItemId,
        CatalogItemId    = s.CatalogItemId,
        Unit             = s.Unit,
        QtyOnHand        = s.QtyOnHand,
        AverageUnitCost  = s.AverageUnitCost
    };

 
    public static StockItem ToNewEntity(this CreateStockItemRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Unit)) throw new ArgumentException("Unit required");
        if (req.CatalogItemId <= 0) throw new ArgumentException("CatalogItemId required");

        return new StockItem
        {
            CatalogItemId    = req.CatalogItemId,
            Unit             = NormalizeUnit(req.Unit),
            QtyOnHand        = req.InitialQtyOnHand.GetValueOrDefault(0m),
            AverageUnitCost  = req.InitialAverageUnitCost.GetValueOrDefault(0m)
        };
    }

    public static void ApplyUpdate(this UpdateStockItemRequest req, StockItem s)
    {
        if (!string.IsNullOrWhiteSpace(req.Unit))
            s.Unit = NormalizeUnit(req.Unit);
       
    }

    private static string NormalizeUnit(string unit)
    {
        var u = unit.Trim();
        return u.Equals("pcs", StringComparison.OrdinalIgnoreCase) ? "pcs" :
            u.Equals("ml",  StringComparison.OrdinalIgnoreCase) ? "ml"  :
            u.Equals("g",   StringComparison.OrdinalIgnoreCase) ? "g"   :
            u; 
    }
}