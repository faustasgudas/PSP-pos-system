namespace PsP.Contracts.Snapshots;

public class DiscountSnapshot
{

       
        public int Version { get; set; } = 1;

       
        public int DiscountId { get; set; }
        public string Code { get; set; } = string.Empty;  

       
        public string Type { get; set; } = string.Empty;  
        public string Scope { get; set; } = string.Empty;  
        public decimal Value { get; set; }

      
        public int? CatalogItemId { get; set; }

       
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }

      
        public DateTime CapturedAtUtc { get; set; }
    
}