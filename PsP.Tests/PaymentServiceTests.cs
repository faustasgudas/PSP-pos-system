using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using PsP.Data;
using PsP.Models;
using PsP.Services.Implementations;
using PsP.Services.Interfaces;
using Stripe.Checkout; // jei pas tave kitas Session tipas – pakeisk

namespace PsP.Tests.Services
{
    public class PaymentServiceTests
    {
        private AppDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new AppDbContext(options);
        }

        private PaymentService CreateService(
            AppDbContext db,
            Mock<IGiftCardService>? giftCardMock = null,
            Mock<StripePaymentService>? stripeMock = null)
        {
            var giftCards = giftCardMock?.Object ?? Mock.Of<IGiftCardService>();
            var stripe    = stripeMock?.Object    ?? Mock.Of<StripePaymentService>();

            return new PaymentService(db, giftCards, stripe);
        }

        // ---------------------------
        // CreatePaymentAsync TESTAI
        // ---------------------------

        [Fact]
        public async Task CreatePaymentAsync_NoGiftCard_UsesStripeOnly()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            await using var db = CreateDbContext(dbName);

            var stripeMock = new Mock<StripePaymentService>();
            stripeMock
                .Setup(s => s.CreateCheckoutSession(
                    It.IsAny<long>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>()))
                .Returns<long, string, string, string, int>((amount, currency, success, cancel, paymentId) =>
                    new Session
                    {
                        Id = "sess_123",
                        Url = "https://stripe.test/checkout"
                    });

            var service = CreateService(db, stripeMock: stripeMock);

            long amountCents = 10_00;
            string currency = "eur";
            int businessId = 1;
            string baseUrl = "https://example.com";

            // Act
            var result = await service.CreatePaymentAsync(
                amountCents,
                currency,
                businessId,
                giftCardCode: null,
                giftCardAmountCents: null,
                baseUrl: baseUrl);

            // Assert
            Assert.Equal(0, result.PaidByGiftCard);
            Assert.Equal(amountCents, result.RemainingForStripe);
            Assert.NotNull(result.StripeUrl);
            Assert.NotNull(result.StripeSessionId);

            var payment = await db.Payments.FirstOrDefaultAsync(p => p.PaymentId == result.PaymentId);
            Assert.NotNull(payment);
            Assert.Equal("Pending", payment!.Status);
            Assert.Equal(amountCents, payment.AmountCents);
            Assert.Equal(currency, payment.Currency);
            Assert.Null(payment.GiftCardId);
            Assert.Equal(result.StripeSessionId, payment.StripeSessionId);

            stripeMock.Verify(s => s.CreateCheckoutSession(
                    amountCents,
                    currency,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    payment.PaymentId),
                Times.Once);
        }

        [Fact]
        public async Task CreatePaymentAsync_InvalidGiftCard_Throws()
        {
            // Arrange
            await using var db = CreateDbContext(Guid.NewGuid().ToString());

            var giftCardMock = new Mock<IGiftCardService>();
            giftCardMock
                .Setup(g => g.GetByCodeAsync("BADCODE"))
                .ReturnsAsync((GiftCard?)null);

            var service = CreateService(db, giftCardMock);

            // Act + Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreatePaymentAsync(
                    amountCents: 10_00,
                    currency: "eur",
                    businessId: 1,
                    giftCardCode: "BADCODE",
                    giftCardAmountCents: null,
                    baseUrl: "https://example.com"));

            Assert.Equal("invalid_gift_card", ex.Message);
        }

        [Fact]
        public async Task CreatePaymentAsync_GiftCardForAnotherBusiness_Throws()
        {
            // Arrange
            await using var db = CreateDbContext(Guid.NewGuid().ToString());

            var card = new GiftCard
            {
                GiftCardId = 1,
                Code = "ABC",
                BusinessId = 999, // kitas business
                Status = "Active",
                Balance = 100_00,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            };

            var giftCardMock = new Mock<IGiftCardService>();
            giftCardMock
                .Setup(g => g.GetByCodeAsync("ABC"))
                .ReturnsAsync(card);

            var service = CreateService(db, giftCardMock);

            // Act + Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreatePaymentAsync(
                    amountCents: 10_00,
                    currency: "eur",
                    businessId: 1,
                    giftCardCode: "ABC",
                    giftCardAmountCents: null,
                    baseUrl: "https://example.com"));

            Assert.Equal("wrong_business", ex.Message);
        }

        [Fact]
        public async Task CreatePaymentAsync_BlockedGiftCard_Throws()
        {
            // Arrange
            await using var db = CreateDbContext(Guid.NewGuid().ToString());

            var card = new GiftCard
            {
                GiftCardId = 1,
                Code = "ABC",
                BusinessId = 1,
                Status = "Blocked", // ne Active
                Balance = 100_00,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            };

            var giftCardMock = new Mock<IGiftCardService>();
            giftCardMock
                .Setup(g => g.GetByCodeAsync("ABC"))
                .ReturnsAsync(card);

            var service = CreateService(db, giftCardMock);

            // Act + Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreatePaymentAsync(
                    amountCents: 10_00,
                    currency: "eur",
                    businessId: 1,
                    giftCardCode: "ABC",
                    giftCardAmountCents: null,
                    baseUrl: "https://example.com"));

            Assert.Equal("blocked", ex.Message);
        }

        [Fact]
        public async Task CreatePaymentAsync_ExpiredGiftCard_Throws()
        {
            // Arrange
            await using var db = CreateDbContext(Guid.NewGuid().ToString());

            var card = new GiftCard
            {
                GiftCardId = 1,
                Code = "ABC",
                BusinessId = 1,
                Status = "Active",
                Balance = 100_00,
                ExpiresAt = DateTime.UtcNow.AddDays(-1) // jau pasibaigusi
            };

            var giftCardMock = new Mock<IGiftCardService>();
            giftCardMock
                .Setup(g => g.GetByCodeAsync("ABC"))
                .ReturnsAsync(card);

            var service = CreateService(db, giftCardMock);

            // Act + Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreatePaymentAsync(
                    amountCents: 10_00,
                    currency: "eur",
                    businessId: 1,
                    giftCardCode: "ABC",
                    giftCardAmountCents: null,
                    baseUrl: "https://example.com"));

            Assert.Equal("expired", ex.Message);
        }

        [Fact]
        public async Task CreatePaymentAsync_FullGiftCardPayment_RedeemsImmediately_NoStripe()
        {
            // Arrange
            await using var db = CreateDbContext(Guid.NewGuid().ToString());

            var card = new GiftCard
            {
                GiftCardId = 1,
                Code = "ABC",
                BusinessId = 1,
                Status = "Active",
                Balance = 100_00,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            };

            var giftCardMock = new Mock<IGiftCardService>();
            giftCardMock
                .Setup(g => g.GetByCodeAsync("ABC"))
                .ReturnsAsync(card);

            giftCardMock
                .Setup(g => g.RedeemAsync(card.GiftCardId, 10_00))
                .Returns(Task.CompletedTask);

            var stripeMock = new Mock<StripePaymentService>(MockBehavior.Strict); // neturėtų būti kviečiamas

            var service = CreateService(db, giftCardMock, stripeMock);

            long amountCents = 10_00;

            // Act
            var result = await service.CreatePaymentAsync(
                amountCents,
                currency: "eur",
                businessId: 1,
                giftCardCode: "ABC",
                giftCardAmountCents: null, // naudos maksimumą
                baseUrl: "https://example.com");

            // Assert
            Assert.Equal(amountCents, result.PaidByGiftCard);
            Assert.Equal(0, result.RemainingForStripe);
            Assert.Null(result.StripeUrl);
            Assert.Null(result.StripeSessionId);

            var payment = await db.Payments.FirstOrDefaultAsync(p => p.PaymentId == result.PaymentId);
            Assert.NotNull(payment);
            Assert.Equal("Success", payment!.Status);
            Assert.NotNull(payment.CompletedAt);
            Assert.Equal(amountCents, payment.GiftCardPlannedCents);

            giftCardMock.Verify(g => g.RedeemAsync(card.GiftCardId, amountCents), Times.Once);
            stripeMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CreatePaymentAsync_PartialGiftCard_RestGoesToStripe()
        {
            // Arrange
            await using var db = CreateDbContext(Guid.NewGuid().ToString());

            var card = new GiftCard
            {
                GiftCardId = 1,
                Code = "ABC",
                BusinessId = 1,
                Status = "Active",
                Balance = 100_00,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            };

            var giftCardMock = new Mock<IGiftCardService>();
            giftCardMock
                .Setup(g => g.GetByCodeAsync("ABC"))
                .ReturnsAsync(card);

            // giftCardAmountCents = 5 EUR, total = 10 EUR
            long amountCents = 10_00;
            long plannedFromGiftCard = 5_00;
            long remainingForStripe = amountCents - plannedFromGiftCard;

            var stripeMock = new Mock<StripePaymentService>();
            stripeMock
                .Setup(s => s.CreateCheckoutSession(
                    remainingForStripe,
                    "eur",
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>()))
                .Returns(new Session
                {
                    Id = "sess_partial",
                    Url = "https://stripe.test/partial"
                });

            var service = CreateService(db, giftCardMock, stripeMock);

            // Act
            var result = await service.CreatePaymentAsync(
                amountCents,
                currency: "eur",
                businessId: 1,
                giftCardCode: "ABC",
                giftCardAmountCents: plannedFromGiftCard,
                baseUrl: "https://example.com");

            // Assert
            Assert.Equal(plannedFromGiftCard, result.PaidByGiftCard);
            Assert.Equal(remainingForStripe, result.RemainingForStripe);
            Assert.NotNull(result.StripeUrl);
            Assert.NotNull(result.StripeSessionId);

            var payment = await db.Payments.FirstOrDefaultAsync(p => p.PaymentId == result.PaymentId);
            Assert.NotNull(payment);
            Assert.Equal("Pending", payment!.Status);
            Assert.Equal(plannedFromGiftCard, payment.GiftCardPlannedCents);
            Assert.Equal("sess_partial", payment.StripeSessionId);

            // RedeemAsync dar neturi būti kviečiamas (tik po sėkmingo Stripe)
            giftCardMock.Verify(g => g.RedeemAsync(It.IsAny<int>(), It.IsAny<long>()), Times.Never);
        }

        // ---------------------------
        // ConfirmStripeSuccessAsync TESTAI
        // ---------------------------

        [Fact]
        public async Task ConfirmStripeSuccessAsync_WithGiftCard_RedeemsAndMarksSuccess()
        {
            // Arrange
            await using var db = CreateDbContext(Guid.NewGuid().ToString());

            var card = new GiftCard
            {
                GiftCardId = 1,
                Code = "ABC",
                BusinessId = 1,
                Status = "Active",
                Balance = 100_00
            };

            var payment = new Payment
            {
                PaymentId = 1,
                AmountCents = 10_00,
                Currency = "eur",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                Method = "GiftCard+Stripe",
                GiftCardId = card.GiftCardId,
                GiftCard = card,
                GiftCardPlannedCents = 5_00,
                StripeSessionId = "sess_123",
                BusinessId = 1
            };

            db.GiftCards.Add(card);
            db.Payments.Add(payment);
            await db.SaveChangesAsync();

            var giftCardMock = new Mock<IGiftCardService>();
            giftCardMock
                .Setup(g => g.RedeemAsync(card.GiftCardId, payment.GiftCardPlannedCents))
                .Returns(Task.CompletedTask);

            var service = CreateService(db, giftCardMock);

            // Act
            await service.ConfirmStripeSuccessAsync("sess_123");

            // Assert
            var updated = await db.Payments.Include(p => p.GiftCard)
                .FirstAsync(p => p.PaymentId == 1);

            Assert.Equal("Success", updated.Status);
            Assert.NotNull(updated.CompletedAt);

            giftCardMock.Verify(g => g.RedeemAsync(card.GiftCardId, payment.GiftCardPlannedCents), Times.Once);
        }

        [Fact]
        public async Task ConfirmStripeSuccessAsync_PaymentNotFound_DoesNothing()
        {
            // Arrange
            await using var db = CreateDbContext(Guid.NewGuid().ToString());

            var giftCardMock = new Mock<IGiftCardService>(MockBehavior.Strict);
            var service = CreateService(db, giftCardMock);

            // Act
            await service.ConfirmStripeSuccessAsync("unknown_session");

            // Assert
            // Jei nepavyko rasti pago – neturi nieko kviesti
            giftCardMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ConfirmStripeSuccessAsync_AlreadySuccess_IsIdempotent()
        {
            // Arrange
            await using var db = CreateDbContext(Guid.NewGuid().ToString());

            var card = new GiftCard
            {
                GiftCardId = 1,
                Code = "ABC",
            };

            var payment = new Payment
            {
                PaymentId = 1,
                AmountCents = 10_00,
                Currency = "eur",
                Status = "Success", // jau success
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                Method = "GiftCard+Stripe",
                GiftCardId = card.GiftCardId,
                GiftCard = card,
                GiftCardPlannedCents = 5_00,
                StripeSessionId = "sess_123",
                BusinessId = 1
            };

            db.GiftCards.Add(card);
            db.Payments.Add(payment);
            await db.SaveChangesAsync();

            var giftCardMock = new Mock<IGiftCardService>(MockBehavior.Strict);
            var service = CreateService(db, giftCardMock);

            // Act
            await service.ConfirmStripeSuccessAsync("sess_123");

            // Assert
            // Dėl idempotentiškumo RedeemAsync neturėtų būti pakartotinai kviečiamas
            giftCardMock.VerifyNoOtherCalls();
        }
    }
}
