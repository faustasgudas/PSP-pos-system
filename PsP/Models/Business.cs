namespace PsP.Models;

public class Business
{
    public int BusinessId { get; set; }
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string CountryCode { get; set; } = null!;
    public bool PriceIncludesTax { get; set; }
    public string BusinessStatus { get; set; } = "Active";

    public List<Payment> Payments { get; set; } = new();

    
}