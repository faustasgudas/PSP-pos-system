using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PsP.Contracts.Businesses;
using PsP.Contracts.GiftCards;
using PsP.Contracts.Payments;
using Xunit;

namespace PsP.Tests; // arba PsP.Tests – pagal tavo projektą

public class ContractsValidationTests
{
    // Helperis bendrai validacijai
    private static IList<ValidationResult> Validate(object model)
    {
        var ctx = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        return results;
    }

    // -------- BUSINESS REQUESTS --------

    [Fact]
    public void CreateBusinessRequest_ValidModel_PassesValidation()
    {
        var req = new CreateBusinessRequest
        {
            Name = "My Biz",
            Address = "Street 1",
            Phone = "+37060000000",
            Email = "test@test.lt",
            CountryCode = "LT",
            PriceIncludesTax = true
        };

        var results = Validate(req);

        Assert.Empty(results);
    }

    [Fact]
    public void CreateBusinessRequest_MissingRequiredFields_FailsValidation()
    {
        var req = new CreateBusinessRequest(); // viskas default -> invalid

        var results = Validate(req);

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBusinessRequest.Name)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBusinessRequest.Address)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBusinessRequest.Phone)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBusinessRequest.Email)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBusinessRequest.CountryCode)));
    }

    [Fact]
    public void CreateBusinessRequest_InvalidEmail_FailsValidation()
    {
        var req = new CreateBusinessRequest
        {
            Name = "Biz",
            Address = "A",
            Phone = "123",
            Email = "not-email",
            CountryCode = "LT"
        };

        var results = Validate(req);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBusinessRequest.Email)));
    }

    [Fact]
    public void CreateBusinessRequest_CountryCode_MustBeExactlyTwoChars()
    {
        var req = new CreateBusinessRequest
        {
            Name = "Biz",
            Address = "A",
            Phone = "123",
            Email = "test@test.lt",
            CountryCode = "L"  // per trumpas
        };

        var results = Validate(req);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBusinessRequest.CountryCode)));
    }

    [Fact]
    public void UpdateBusinessRequest_ValidModel_PassesValidation()
    {
        var req = new UpdateBusinessRequest
        {
            Name = "Biz Updated",
            Address = "New addr",
            Phone = "+37061111111",
            Email = "new@test.lt",
            CountryCode = "LT",
            PriceIncludesTax = false
        };

        var results = Validate(req);

        Assert.Empty(results);
    }

    // -------- GIFT CARD REQUESTS --------

    [Fact]
    public void CreateGiftCardRequest_ValidModel_PassesValidation()
    {
        var req = new CreateGiftCardRequest
        {
            BusinessId = 1,
            Code = "CARD-001",
            Balance = 1000,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        var results = Validate(req);

        Assert.Empty(results);
    }

    [Fact]
    public void CreateGiftCardRequest_RequiresBusinessId_AndCode()
    {
        var req = new CreateGiftCardRequest
        {
            BusinessId = 0, // invalid dėl [Range(1, ...)]
            Code = ""       // invalid dėl [Required]/[StringLength]
        };

        var results = Validate(req);

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateGiftCardRequest.BusinessId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateGiftCardRequest.Code)));
    }

    [Fact]
    public void CreateGiftCardRequest_AllowsZeroBalance()
    {
        var req = new CreateGiftCardRequest
        {
            BusinessId = 1,
            Code = "ZERO",
            Balance = 0
        };

        var results = Validate(req);

        Assert.Empty(results); // [Range(0, long.MaxValue)] leidžia 0
    }

    [Fact]
    public void RedeemRequest_AmountMustBePositive()
    {
        var req = new RedeemRequest
        {
            Amount = 0 // turi feilin't
        };

        var results = Validate(req);

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RedeemRequest.Amount)));
    }

    [Fact]
    public void UpdateBalanceRequest_AmountMustBePositive()
    {
        var req = new UpdateBalanceRequest
        {
            Amount = -5
        };

        var results = Validate(req);

        Assert.NotEmpty(results);
    }

    // -------- PAYMENTS --------

    [Fact]
    public void CreatePaymentRequest_ValidModel_PassesValidation()
    {
        var req = new CreatePaymentRequest
        {
            AmountCents = 1234,
            Currency = "eur",
            BusinessId = 1,
            OrderId = 10,
            GiftCardCode = null,
            GiftCardAmountCents = null
        };

        var results = Validate(req);

        Assert.Empty(results);
    }

    [Fact]
    public void CreatePaymentRequest_AmountMustBePositive()
    {
        var req = new CreatePaymentRequest
        {
            AmountCents = 0,
            Currency = "eur",
            BusinessId = 1,
            OrderId = 1
        };

        var results = Validate(req);

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreatePaymentRequest.AmountCents)));
    }

    [Fact]
    public void CreatePaymentRequest_BusinessId_And_OrderId_MustBePositive()
    {
        var req = new CreatePaymentRequest
        {
            AmountCents = 10,
            Currency = "eur",
            BusinessId = 0,
            OrderId = 0
        };

        var results = Validate(req);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreatePaymentRequest.BusinessId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreatePaymentRequest.OrderId)));
    }

    [Fact]
    public void CreatePaymentRequest_GiftCardAmountIfPresent_MustBePositive()
    {
        var req = new CreatePaymentRequest
        {
            AmountCents = 1000,
            Currency = "eur",
            BusinessId = 1,
            OrderId = 1,
            GiftCardCode = "GC1",
            GiftCardAmountCents = 0 // invalid
        };

        var results = Validate(req);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreatePaymentRequest.GiftCardAmountCents)));
    }
}
