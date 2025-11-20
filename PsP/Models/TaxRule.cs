namespace PsP.Models;

public class TaxRule
{
    public int TaxRuleId { get; set; }

    public string CountryCode { get; set; } = null!;   // pvz. "LT", "LV"
    public string TaxClass { get; set; } = null!;      // pvz. "Food", "Alcohol"
    public decimal RatePercent { get; set; }           // pvz. 21.0m

    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
}