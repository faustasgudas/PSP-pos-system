using System.ComponentModel.DataAnnotations;

namespace PsP.Contracts.GiftCards;

public class RedeemRequest
{
    [Range(1, long.MaxValue)]
    public long Amount { get; set; }
}