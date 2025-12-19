namespace PsP.Models;

public class Discount
{
    public int DiscountId { get; set; }

    public string Code { get; set; } = null!;          
    public string Type { get; set; } = null!;         
    public string Scope { get; set; } = null!;        
    public decimal Value { get; set; }                

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }

    public string Status { get; set; } = "Active";    
    
    public int BusinessId { get; set; }
    public Business Business { get; set; } = null!;

    public List<DiscountEligibility> Eligibilities { get; set; } = new();
}