namespace PsP.Contracts.GiftCards;

public class RedeemResponse
{
    public long Charged { get; set; }
    public long RemainingBalance { get; set; }

    public RedeemResponse(long charged, long remainingBalance)
    {
        Charged = charged;
        RemainingBalance = remainingBalance;
    }
}