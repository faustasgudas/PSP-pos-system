namespace PsP.Contracts.Tax;

public class CreateTaxRuleRequest
{
    public string CountryCode { get; set; } = null!;
    public string TaxClass { get; set; } = null!;  
    public decimal RatePercent { get; set; }      
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
}