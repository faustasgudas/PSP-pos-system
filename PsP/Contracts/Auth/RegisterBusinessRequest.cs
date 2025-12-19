namespace PsP.Contracts.Auth;

public class RegisterBusinessRequest
{
    public string BusinessName { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string CountryCode { get; set; } = "LT";
    public bool PriceIncludesTax { get; set; }

    public string OwnerName { get; set; } = null!;
    public string OwnerEmail { get; set; } = null!;
    public string OwnerPassword { get; set; } = null!;
    
    public string BusinessType { get; set; }

}