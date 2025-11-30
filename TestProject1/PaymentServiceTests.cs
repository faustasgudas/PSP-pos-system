using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PsP.Contracts.GiftCards;
using PsP.Models;
using PsP.Services.Implementations;
using TestProject1; // TestHelpers
using Xunit;

namespace PsP.Tests;

public class PaymentServiceTests
{
    private PsP.Data.AppDbContext NewDb() => TestHelpers.NewInMemoryContext();

    private (PaymentService payment, GiftCardService giftCards, PsP.Data.AppDbContext db) CreateServices()
    {
        var db = NewDb();
        var gift = new GiftCardService(db);
        // Stripe mock'inti kol kas nenorim – testuose nesirenkam kelių, kurie jį kviestų
        StripePaymentService stripe = null!;
        var payment = new PaymentService(db, gift, stripe);
        return (payment, gift, db);
    }

    private async Task<(Business biz, Employee emp, Order order)> SeedBasicOrder(PsP.Data.AppDbContext db)
    {
        var (biz, emp) = TestHelpers.SeedBusinessAndEmployee(db);

        var order = new Order
        {
            BusinessId = biz.BusinessId,
            EmployeeId = emp.EmployeeId,
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return (biz, emp, order);
    }

    [Fact]
    public async Task CreatePaymentAsync_Throws_WhenAmountNonPositive()
    {
        var (payment, _, db) = CreateServices();
        var (biz, _, order) = await SeedBasicOrder(db);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            payment.CreatePaymentAsync(
                amountCents: 0,
                currency: "eur",
                businessId: biz.BusinessId,
                orderId: order.OrderId,
                giftCardCode: null,
                giftCardAmountCents: null,
                baseUrl: "https://test.local")
        );
    }

    [Fact]
    public async Task CreatePaymentAsync_Throws_WhenOrderNotFound()
    {
        var (payment, _, db) = CreateServices();
        var (biz, _) = TestHelpers.SeedBusinessAndEmployee(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            payment.CreatePaymentAsync(
                amountCents: 1000,
                currency: "eur",
                businessId: biz.BusinessId,
                orderId: 999999,
                giftCardCode: null,
                giftCardAmountCents: null,
                baseUrl: "https://test.local")
        );
    }

    [Fact]
    public async Task CreatePaymentAsync_Throws_WhenOrderWrongBusiness()
    {
        var (payment, _, db) = CreateServices();

        var (biz1, emp1) = TestHelpers.SeedBusinessAndEmployee(db);
        var (biz2, emp2) = TestHelpers.SeedBusinessAndEmployee(db);

        var order = new Order
        {
            BusinessId = biz2.BusinessId,
            EmployeeId = emp2.EmployeeId,
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            payment.CreatePaymentAsync(
                amountCents: 1000,
                currency: "eur",
                businessId: biz1.BusinessId, // neteisingas business
                orderId: order.OrderId,
                giftCardCode: null,
                giftCardAmountCents: null,
                baseUrl: "https://test.local")
        );
    }

    [Fact]
    public async Task CreatePaymentAsync_Throws_WhenGiftCardAmountNonPositive()
    {
        var (payment, giftCards, db) = CreateServices();
        var (biz, _, order) = await SeedBasicOrder(db);

        var card = await giftCards.CreateAsync(new GiftCard
        {
            BusinessId = biz.BusinessId,
            Code = "GC1",
            Balance = 1000,
            Status = "Active"
        });

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            payment.CreatePaymentAsync(
                amountCents: 1000,
                currency: "eur",
                businessId: biz.BusinessId,
                orderId: order.OrderId,
                giftCardCode: card.Code,
                giftCardAmountCents: 0,
                baseUrl: "https://test.local")
        );
    }

    [Fact]
    public async Task CreatePaymentAsync_GiftCardOnly_FullAmountCovered_SetsSuccess_AndRedeems()
    {
        var (payment, giftCards, db) = CreateServices();
        var (biz, _, order) = await SeedBasicOrder(db);

        var card = await giftCards.CreateAsync(new GiftCard
        {
            BusinessId = biz.BusinessId,
            Code = "GC_FULL",
            Balance = 2000,
            Status = "Active"
        });

        var amount = 1500L;

        var response = await payment.CreatePaymentAsync(
            amountCents: amount,
            currency: "eur",
            businessId: biz.BusinessId,
            orderId: order.OrderId,
            giftCardCode: card.Code,
            giftCardAmountCents: null, // max kiek galima
            baseUrl: "https://test.local");

        // Stripe nenaudojamas -> Payment turi būti Success iš karto
        var paymentFromDb = await db.Payments.SingleAsync();

        Assert.Equal(amount, paymentFromDb.AmountCents);
        Assert.Equal("GiftCard", paymentFromDb.Method);
        Assert.Equal("Success", paymentFromDb.Status);
        Assert.Equal(card.GiftCardId, paymentFromDb.GiftCardId);
        Assert.Equal(amount, paymentFromDb.GiftCardPlannedCents);
        Assert.NotNull(paymentFromDb.CompletedAt);
        Assert.Null(paymentFromDb.StripeSessionId);

        var cardFromDb = await db.GiftCards.FindAsync(card.GiftCardId);
        Assert.Equal(2000 - amount, cardFromDb!.Balance);

    }

    [Fact]
    public async Task ConfirmStripeSuccess_NoPayment_DoesNothing()
    {
        var (payment, _, db) = CreateServices();

        await payment.ConfirmStripeSuccessAsync("non-existent");

        var count = await db.Payments.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ConfirmStripeSuccess_AlreadySuccess_IsIdempotent()
    {
        var (payment, giftCards, db) = CreateServices();
        var (biz, _, order) = await SeedBasicOrder(db);

        var card = await giftCards.CreateAsync(new GiftCard
        {
            BusinessId = biz.BusinessId,
            Code = "GC_IDEMP",
            Balance = 1000,
            Status = "Active"
        });

        var p = new Payment
        {
            BusinessId = biz.BusinessId,
            OrderId = order.OrderId,
            AmountCents = 500,
            Currency = "eur",
            Status = "Success",
            Method = "GiftCard+Stripe",
            GiftCardId = card.GiftCardId,
            GiftCardPlannedCents = 300,
            StripeSessionId = "sess_123",
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };
        db.Payments.Add(p);
        await db.SaveChangesAsync();

        await payment.ConfirmStripeSuccessAsync("sess_123");

        var fromDb = await db.Payments.SingleAsync();
        Assert.Equal("Success", fromDb.Status); // nepasikeitė
    }

    [Fact]
    public async Task ConfirmStripeSuccess_WithGiftCard_ChargesGiftCard_AndUpdatesPayment()
    {
        var (payment, giftCards, db) = CreateServices();
        var (biz, _, order) = await SeedBasicOrder(db);

        var card = await giftCards.CreateAsync(new GiftCard
        {
            BusinessId = biz.BusinessId,
            Code = "GC_STRIPE",
            Balance = 1000,
            Status = "Active"
        });

        var p = new Payment
        {
            BusinessId = biz.BusinessId,
            OrderId = order.OrderId,
            AmountCents = 1500,
            Currency = "eur",
            Status = "Pending",
            Method = "GiftCard+Stripe",
            GiftCardId = card.GiftCardId,
            GiftCardPlannedCents = 400,
            StripeSessionId = "sess_gc",
            CreatedAt = DateTime.UtcNow
        };
        db.Payments.Add(p);
        await db.SaveChangesAsync();

        await payment.ConfirmStripeSuccessAsync("sess_gc");

        var paymentFromDb = await db.Payments.SingleAsync();
        Assert.Equal("Success", paymentFromDb.Status);
        Assert.NotNull(paymentFromDb.CompletedAt);
        Assert.Equal(400, paymentFromDb.GiftCardPlannedCents);

        var cardFromDb = await db.GiftCards.FindAsync(card.GiftCardId);
        Assert.Equal(1000 - 400, cardFromDb!.Balance);
    }

    [Fact]
    public async Task ConfirmStripeSuccess_WhenGiftCardRedeemFails_StillMarksPaymentSuccess()
    {
        var (payment, giftCards, db) = CreateServices();
        var (biz, _, order) = await SeedBasicOrder(db);

        // Kortelė bus Inactive, kad RedeemAsync mestų "blocked"
        var card = await giftCards.CreateAsync(new GiftCard
        {
            BusinessId = biz.BusinessId,
            Code = "GC_FAIL",
            Balance = 1000,
            Status = "Inactive"
        });

        var p = new Payment
        {
            BusinessId = biz.BusinessId,
            OrderId = order.OrderId,
            AmountCents = 1500,
            Currency = "eur",
            Status = "Pending",
            Method = "GiftCard+Stripe",
            GiftCardId = card.GiftCardId,
            GiftCardPlannedCents = 400,
            StripeSessionId = "sess_fail",
            CreatedAt = DateTime.UtcNow
        };
        db.Payments.Add(p);
        await db.SaveChangesAsync();

        await payment.ConfirmStripeSuccessAsync("sess_fail");

        var paymentFromDb = await db.Payments.SingleAsync();
        Assert.Equal("Success", paymentFromDb.Status);
        Assert.NotNull(paymentFromDb.CompletedAt);

        var cardFromDb = await db.GiftCards.FindAsync(card.GiftCardId);
        Assert.Equal(1000, cardFromDb!.Balance);
    }
    
    [Fact]
    public async Task CreatePaymentAsync_Throws_WhenGiftCardCodeInvalid()
    {
        var (payment, _, db) = CreateServices();
        var (biz, _, order) = await SeedBasicOrder(db);

        // DB nera nei vienos gift card, bet nurodom kodą
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            payment.CreatePaymentAsync(
                amountCents: 1000,
                currency: "eur",
                businessId: biz.BusinessId,
                orderId: order.OrderId,
                giftCardCode: "NO_SUCH_CARD",
                giftCardAmountCents: null,
                baseUrl: "https://test.local")
        );

        Assert.Equal("invalid_gift_card", ex.Message);
    }
    [Fact]
    public async Task CreatePaymentAsync_Throws_WhenGiftCardFromOtherBusiness()
    {
        var (payment, giftCards, db) = CreateServices();

        var (biz1, _, order) = await SeedBasicOrder(db);
        var (biz2, _) = TestHelpers.SeedBusinessAndEmployee(db); // antras biz

        var card = await giftCards.CreateAsync(new GiftCard
        {
            BusinessId = biz2.BusinessId,  // kito biz
            Code = "GC_OTHER",
            Balance = 1000,
            Status = "Active"
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            payment.CreatePaymentAsync(
                amountCents: 1000,
                currency: "eur",
                businessId: biz1.BusinessId,
                orderId: order.OrderId,
                giftCardCode: card.Code,
                giftCardAmountCents: null,
                baseUrl: "https://test.local")
        );

        Assert.Equal("wrong_business", ex.Message);
    }
    [Fact]
    public async Task CreatePaymentAsync_Throws_WhenGiftCardBlocked()
    {
        var (payment, giftCards, db) = CreateServices();
        var (biz, _, order) = await SeedBasicOrder(db);

        var card = await giftCards.CreateAsync(new GiftCard
        {
            BusinessId = biz.BusinessId,
            Code = "GC_BLOCKED",
            Balance = 1000,
            Status = "Inactive"   // svarbu
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            payment.CreatePaymentAsync(
                amountCents: 1000,
                currency: "eur",
                businessId: biz.BusinessId,
                orderId: order.OrderId,
                giftCardCode: card.Code,
                giftCardAmountCents: null,
                baseUrl: "https://test.local")
        );

        Assert.Equal("blocked", ex.Message);
    }
    [Fact]
    public async Task CreatePaymentAsync_Throws_WhenGiftCardExpired()
    {
        var (payment, giftCards, db) = CreateServices();
        var (biz, _, order) = await SeedBasicOrder(db);

        var card = await giftCards.CreateAsync(new GiftCard
        {
            BusinessId = biz.BusinessId,
            Code = "GC_EXPIRED",
            Balance = 1000,
            Status = "Active",
            ExpiresAt = DateTime.UtcNow.AddDays(-1) // vakar
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            payment.CreatePaymentAsync(
                amountCents: 1000,
                currency: "eur",
                businessId: biz.BusinessId,
                orderId: order.OrderId,
                giftCardCode: card.Code,
                giftCardAmountCents: null,
                baseUrl: "https://test.local")
        );

        Assert.Equal("expired", ex.Message);
    }

}
