namespace PsP.Models;

public class CatalogItem
{
    public int CatalogItemId { get; set; }

  
    public int BusinessId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;   
    public string Type { get; set; } = string.Empty;   
    public decimal BasePrice { get; set; }
    public string Status { get; set; } = string.Empty; 
    public int DefaultDurationMin { get; set; }        
    public string TaxClass { get; set; } = string.Empty; 

   
    public Business? Business { get; set; }

    public ICollection<DiscountEligibility> DiscountEligibilities { get; set; } = new List<DiscountEligibility>();


    
}