using Microsoft.EntityFrameworkCore;
using PsP.Contracts.Orders;
using PsP.Data;
using PsP.Models;
using PsP.Services.Implementations;
using PsP.Services.Interfaces;

namespace TestProject1;

public class OrdersServiceTests
{
   private static (AppDbContext db, IDiscountsService discSvc, IOrdersService orderSvc) NewSystem()
        {
            var db = TestHelpers.NewContext();
            var discSvc = new DiscountsService(db);
            var stocks = new StockMovementService(db);
            var orderSvc = new OrdersService(db, discSvc,stocks); // assuming your ctor is (AppDbContext, IDiscountsService)
            return (db, discSvc, orderSvc);
        }

        [Fact]
        public async Task CreateOrder_AppliesNewestOrderDiscount_WhenAvailable()
        {
            var (db, discSvc, svc) = NewSystem();
            var (biz, emp) = TestHelpers.SeedBusinessAndEmployee(db);

            // Active order-level discount (newest wins)
            var older = new Discount {
                BusinessId = biz.BusinessId, Code="ORD10", Type="Percent", Scope="Order", Value=10m,
                StartsAt = DateTime.UtcNow.AddDays(-10), EndsAt = DateTime.UtcNow.AddDays(10), Status="Active"
            };
            var newer = new Discount {
                BusinessId = biz.BusinessId, Code="ORD12", Type="Percent", Scope="Order", Value=12m,
                StartsAt = DateTime.UtcNow.AddDays(-1), EndsAt = DateTime.UtcNow.AddDays(10), Status="Active"
            };
            db.Discounts.AddRange(older, newer);
            await db.SaveChangesAsync();

            var req = new CreateOrderRequest
            {
                EmployeeId = emp.EmployeeId,
                ReservationId = null,
                TableOrArea = "A1"
            };

            var dto = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,req, CancellationToken.None);

            // assert: the *newest* order discount was captured
            var order = await db.Orders.AsNoTracking().FirstAsync(o => o.OrderId == dto.OrderId);
            Assert.Equal(newer.DiscountId, order.DiscountId);
            Assert.False(string.IsNullOrWhiteSpace(order.OrderDiscountSnapshot));
        }

        [Fact]
        public async Task CreateOrder_NoDiscount_WhenNoneEligible()
        {
            var (db, discSvc, svc) = NewSystem();
            var (biz, emp) = TestHelpers.SeedBusinessAndEmployee(db);

            // Discount out of window -> not eligible
            db.Discounts.Add(new Discount {
                BusinessId = biz.BusinessId, Code="OLD", Type="Amount", Scope="Order", Value=5m,
                StartsAt = DateTime.UtcNow.AddDays(-20), EndsAt = DateTime.UtcNow.AddDays(-10), Status="Active"
            });
            await db.SaveChangesAsync();

            var req = new CreateOrderRequest { EmployeeId = emp.EmployeeId, TableOrArea = "B2" };
            var dto = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,req, CancellationToken.None);

            var order = await db.Orders.AsNoTracking().FirstAsync(o => o.OrderId == dto.OrderId);
            Assert.Null(order.DiscountId);
            Assert.True(string.IsNullOrWhiteSpace(order.OrderDiscountSnapshot));
        }

        [Fact]
        public async Task AddLine_AutoAppliesNewestEligibleLineDiscount()
        {
            var (db, _, svc) = NewSystem();
            var (biz, emp) = TestHelpers.SeedBusinessAndEmployee(db);
            var item = TestHelpers.SeedCatalogItem(db, biz.BusinessId);

            // Eligible line discount (and an older one so we verify "newest" wins)
            var older = new Discount {
                BusinessId = biz.BusinessId, Code="ITEM5", Type="Percent", Scope="Line", Value=5m,
                StartsAt = DateTime.UtcNow.AddDays(-5), EndsAt = DateTime.UtcNow.AddDays(10), Status="Active",
                Eligibilities = { new DiscountEligibility { CatalogItemId = item.CatalogItemId } }
            };
            var newer = new Discount {
                BusinessId = biz.BusinessId, Code="ITEM8", Type="Percent", Scope="Line", Value=8m,
                StartsAt = DateTime.UtcNow.AddDays(-1), EndsAt = DateTime.UtcNow.AddDays(10), Status="Active",
                Eligibilities = { new DiscountEligibility { CatalogItemId = item.CatalogItemId } }
            };
            db.Discounts.AddRange(older, newer);
            await db.SaveChangesAsync();

            // Create order
            var dto = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,new CreateOrderRequest
            {
                EmployeeId = emp.EmployeeId,
                TableOrArea = "C3"
            }, CancellationToken.None);

            // Add line -> should auto-apply newest eligible line discount
            var addReq = new AddLineRequest { CatalogItemId = item.CatalogItemId, Qty = 2m };
            var lineDto = await svc.AddLineAsync(biz.BusinessId, dto.OrderId, emp.EmployeeId, addReq, CancellationToken.None);

            var line = await db.OrderLines.AsNoTracking().FirstAsync(l => l.OrderLineId == lineDto.OrderLineId);
            Assert.Equal(newer.DiscountId, line.DiscountId);
            Assert.False(string.IsNullOrWhiteSpace(line.UnitDiscountSnapshot));
        }

        [Fact]
        public async Task UpdateLine_OverrideDiscount_SetsNewSnapshot()
        {
            var (db, _, svc) = NewSystem();
            var (biz, emp) = TestHelpers.SeedBusinessAndEmployee(db);
            var item = TestHelpers.SeedCatalogItem(db, biz.BusinessId);

            // Two eligible line discounts
            var baseDisc = new Discount {
                BusinessId = biz.BusinessId, Code="ITEM5", Type="Percent", Scope="Line", Value=5m,
                StartsAt = DateTime.UtcNow.AddDays(-3), EndsAt = DateTime.UtcNow.AddDays(10), Status="Active",
                Eligibilities = { new DiscountEligibility { CatalogItemId = item.CatalogItemId } }
            };
            var overrideDisc = new Discount {
                BusinessId = biz.BusinessId, Code="ITEM15", Type="Percent", Scope="Line", Value=15m,
                StartsAt = DateTime.UtcNow.AddDays(-2), EndsAt = DateTime.UtcNow.AddDays(10), Status="Active",
                Eligibilities = { new DiscountEligibility { CatalogItemId = item.CatalogItemId } }
            };
            db.Discounts.AddRange(baseDisc, overrideDisc);
            await db.SaveChangesAsync();

            // Order + initial line (auto applies newest = overrideDisc)
            var order = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,new CreateOrderRequest
            {
                EmployeeId = emp.EmployeeId
            }, CancellationToken.None);

            var line = await svc.AddLineAsync(biz.BusinessId, order.OrderId, emp.EmployeeId,
                new AddLineRequest { CatalogItemId = item.CatalogItemId, Qty = 1m }, CancellationToken.None);

            // Now explicitly override with baseDisc (older / different)
            await svc.UpdateLineAsync(
                biz.BusinessId, order.OrderId, line.OrderLineId, emp.EmployeeId,
                new UpdateLineRequest { DiscountId = baseDisc.DiscountId, Qty = 2m },
                CancellationToken.None);

            var reloaded = await db.OrderLines.AsNoTracking().FirstAsync(l => l.OrderLineId == line.OrderLineId);
            Assert.Equal(baseDisc.DiscountId, reloaded.DiscountId);
            Assert.False(string.IsNullOrWhiteSpace(reloaded.UnitDiscountSnapshot));
            Assert.Equal(2m, reloaded.Qty);
        }

        [Fact]
        public async Task UpdateLine_ClearDiscount_RemovesSnapshot()
        {
            var (db, _, svc) = NewSystem();
            var (biz, emp) = TestHelpers.SeedBusinessAndEmployee(db);
            var item = TestHelpers.SeedCatalogItem(db, biz.BusinessId);

            // Eligible discount present initially
            db.Discounts.Add(new Discount {
                BusinessId = biz.BusinessId, Code="ITEM10", Type="Percent", Scope="Line", Value=10m,
                StartsAt = DateTime.UtcNow.AddDays(-1), EndsAt = DateTime.UtcNow.AddDays(10), Status="Active",
                Eligibilities = { new DiscountEligibility { CatalogItemId = item.CatalogItemId } }
            });
            await db.SaveChangesAsync();

            // Order + line (auto applies)
            var order = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,new CreateOrderRequest { EmployeeId = emp.EmployeeId }, CancellationToken.None);
            var line = await svc.AddLineAsync(biz.BusinessId, order.OrderId, emp.EmployeeId,
                new AddLineRequest { CatalogItemId = item.CatalogItemId, Qty = 1m }, CancellationToken.None);

            // Clear discount
            await svc.UpdateLineAsync(
                biz.BusinessId, order.OrderId, line.OrderLineId, emp.EmployeeId,
                new UpdateLineRequest { DiscountId = null }, CancellationToken.None);

            var reloaded = await db.OrderLines.AsNoTracking().FirstAsync(l => l.OrderLineId == line.OrderLineId);
            Assert.Null(reloaded.DiscountId);
            Assert.True(string.IsNullOrWhiteSpace(reloaded.UnitDiscountSnapshot));
        }

        [Fact]
        public async Task UpdateOrder_SetDiscountId_ReplacesSnapshot()
        {
            var (db, _, svc) = NewSystem();
            var (biz, emp) = TestHelpers.SeedBusinessAndEmployee(db);

            // Two order-level discounts
            var d1 = new Discount {
                BusinessId = biz.BusinessId, Code="ORD5", Type="Amount", Scope="Order", Value=5m,
                StartsAt = DateTime.UtcNow.AddDays(-2), EndsAt = DateTime.UtcNow.AddDays(10), Status="Active"
            };
            var d2 = new Discount {
                BusinessId = biz.BusinessId, Code="ORD20", Type="Amount", Scope="Order", Value=20m,
                StartsAt = DateTime.UtcNow.AddDays(-1), EndsAt = DateTime.UtcNow.AddDays(10), Status="Active"
            };
            db.Discounts.AddRange(d1, d2);
            await db.SaveChangesAsync();

            var order = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,new CreateOrderRequest { EmployeeId = emp.EmployeeId }, CancellationToken.None);

           
            await svc.UpdateOrderAsync(biz.BusinessId, order.OrderId, emp.EmployeeId,new UpdateOrderRequest
            {
                EmployeeId = emp.EmployeeId,
                DiscountId = d1.DiscountId
            }, CancellationToken.None);

            var reloaded = await db.Orders.AsNoTracking().FirstAsync(o => o.OrderId == order.OrderId);
            Assert.Equal(d1.DiscountId, reloaded.DiscountId);
            Assert.False(string.IsNullOrWhiteSpace(reloaded.OrderDiscountSnapshot));
        }
        
        
        
        
        
}
