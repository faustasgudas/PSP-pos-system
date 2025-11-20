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
                Lines = lines.Select(l => l.ToLineResponse()).ToList()
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
        public static Order ToNewEntity(this CreateOrderRequest req, int businessId) =>
            new Order
            {
                BusinessId = businessId,
                EmployeeId = req.EmployeeId,
                ReservationId = req.ReservationId,
                TableOrArea = req.TableOrArea,
                Status = "Open",
                CreatedAt = DateTime.UtcNow,
                TipAmount = 0m
            };
}