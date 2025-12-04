using PsP.Contracts.Orders;
using PsP.Models;

namespace PsP.Mappings;

public static class OrderMappings
{
    public static OrderSummaryResponse ToSummaryResponse(this Order o) =>
            new OrderSummaryResponse
            {
                OrderId = o.OrderId,
                BusinessId = o.BusinessId,
                EmployeeId = o.EmployeeId,
                ReservationId = o.ReservationId,
                Status = o.Status,
                TableOrArea = o.TableOrArea,
                CreatedAt = o.CreatedAt,
                ClosedAt = o.ClosedAt,
                TipAmount = o.TipAmount,
                DiscountId = o.DiscountId
            };

        public static OrderDetailResponse ToDetailResponse(this Order o, IEnumerable<OrderLine> lines) =>
            new OrderDetailResponse
            {
                OrderId = o.OrderId,
                BusinessId = o.BusinessId,
                EmployeeId = o.EmployeeId,
                ReservationId = o.ReservationId,
                Status = o.Status,
                TableOrArea = o.TableOrArea,
                CreatedAt = o.CreatedAt,
                ClosedAt = o.ClosedAt,
                TipAmount = o.TipAmount,
                DiscountId = o.DiscountId,
                OrderDiscountSnapshot = o.OrderDiscountSnapshot,
                Lines = (o.Lines ?? Enumerable.Empty<OrderLine>()).Select(l => l.ToLineResponse()).ToList()
            };

        public static OrderLineResponse ToLineResponse(this OrderLine l) =>
            new OrderLineResponse
            {
                OrderLineId = l.OrderLineId,
                OrderId = l.OrderId,
                BusinessId = l.BusinessId,
                CatalogItemId = l.CatalogItemId,
                DiscountId = l.DiscountId,
                Qty = l.Qty,
                ItemNameSnapshot = l.ItemNameSnapshot,
                UnitPriceSnapshot = l.UnitPriceSnapshot,
                UnitDiscountSnapshot = l.UnitDiscountSnapshot,
                TaxClassSnapshot = l.TaxClassSnapshot,
                TaxRateSnapshotPct = l.TaxRateSnapshotPct,
                PerformedAt = l.PerformedAt,
                PerformedByEmployeeId = l.PerformedByEmployeeId
            };

        // Input mapping (Request -> Entity)
        public static Order ToNewEntity(this CreateOrderRequest req, int businessId, int? discountId, string? discountSnapshot) =>
            new Order
            {
                BusinessId = businessId,
                EmployeeId = req.EmployeeId,
                ReservationId = req.ReservationId,
                TableOrArea = req.TableOrArea,
                Status = "Open",
                CreatedAt = DateTime.UtcNow,
                TipAmount = 0m,
                DiscountId = discountId,
                OrderDiscountSnapshot = discountSnapshot
            };
        
        public static void ApplyUpdate(this UpdateOrderRequest req, Order o)
            {
                if (req.Status is not null)      o.Status = req.Status;
                if (req.TableOrArea is not null) o.TableOrArea = req.TableOrArea;
        
                if (req.TipAmount is not null && decimal.TryParse(req.TipAmount, out var tip))
                    o.TipAmount = tip;
        
                o.DiscountId = req.DiscountId;
                // If you also create/refresh OrderDiscountSnapshot, do it in the service before saving.
                o.EmployeeId = req.EmployeeId;
            }
        
            // Order: apply "close" action (controller already ensures it's allowed)
            public static void ApplyClose(this Order o)
            {
                
                o.Status = "Closed";
                o.ClosedAt = DateTime.UtcNow;
            }
        
            // Order: apply "cancel" action
            public static void ApplyCancel(this Order o)
            {
                o.Status = "Cancelled";
                o.ClosedAt = DateTime.UtcNow;
                // If you log a cancel reason, do it in a separate audit table (no field on Order).
            }
            public static void ApplyRefund(this Order o)
            {
                o.Status = "Refunded";
                
            }
            // OrderLine: create from AddLineRequest + server-resolved snapshots
            public static OrderLine ToNewLineEntity(
                this AddLineRequest req,
                int businessId,
                int orderId,
                int performedByEmployeeId,
                string itemNameSnapshot,
                decimal unitPriceSnapshot,
                string taxClassSnapshot,
                decimal taxRateSnapshotPct,
                string? unitDiscountSnapshot = null,
                DateTime? nowUtc = null)
            {
                return new OrderLine
                {
                    BusinessId           = businessId,
                    OrderId              = orderId,
                    CatalogItemId        = req.CatalogItemId,
                    DiscountId           = req.DiscountId,
                    Qty                  = req.Qty,
        
                    // snapshots resolved by your domain/service layer
                    ItemNameSnapshot     = itemNameSnapshot,
                    UnitPriceSnapshot    = unitPriceSnapshot,
                    TaxClassSnapshot     = taxClassSnapshot,
                    TaxRateSnapshotPct   = taxRateSnapshotPct,
                    UnitDiscountSnapshot = unitDiscountSnapshot,
        
                    // audit
                    PerformedAt          = nowUtc ?? DateTime.UtcNow,
                    PerformedByEmployeeId = performedByEmployeeId
                };
            }
        
            // OrderLine: apply updates from UpdateLineRequest (+ optional refreshed discount snapshot)
            public static void ApplyUpdate(
                this UpdateLineRequest req,
                OrderLine line,
                int performedByEmployeeId,
                DateTime? nowUtc = null,
                string? unitDiscountSnapshot = null)
            {
                if (req.Qty.HasValue)      line.Qty = req.Qty.Value;
                line.DiscountId = req.DiscountId; // set or clear
                line.UnitDiscountSnapshot = unitDiscountSnapshot;
        
                // audit
                line.PerformedByEmployeeId = performedByEmployeeId;
                line.PerformedAt = nowUtc ?? DateTime.UtcNow;
            }
        
            // Convenience: collections
            public static IEnumerable<OrderSummaryResponse> ToSummaryResponses(this IEnumerable<Order> orders) =>
                orders.Select(o => o.ToSummaryResponse());
        
            public static IEnumerable<OrderLineResponse> ToLineResponses(this IEnumerable<OrderLine> lines) =>
                lines.Select(l => l.ToLineResponse());
        
        
        
        
        
}