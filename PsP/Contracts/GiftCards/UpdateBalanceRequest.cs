using System.ComponentModel.DataAnnotations;

namespace PsP.Contracts.GiftCards;

public class UpdateBalanceRequest
{
    [Range(1, long.MaxValue)]
    public long Amount { get; set; }
}