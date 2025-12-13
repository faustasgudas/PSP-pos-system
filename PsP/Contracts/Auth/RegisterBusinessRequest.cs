namespace PsP.Contracts.Auth;

public class RegisterBusinessRequest
{
    // Business info
    public string BusinessName { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Email { get; set; } = null!;      // Business email
    public string CountryCode { get; set; } = "LT";
    public bool PriceIncludesTax { get; set; }

    // Owner info
    public string OwnerName { get; set; } = null!;
    public string OwnerEmail { get; set; } = null!;
    public string OwnerPassword { get; set; } = null!;
    
    public string BusinessType { get; set; }

}