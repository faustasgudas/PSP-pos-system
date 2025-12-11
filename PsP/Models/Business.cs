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
        
    public string BusinessType { get; set; } = "Catering";
    public ICollection<Payment> Payments { get; set; } =new List<Payment>();

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public ICollection<CatalogItem> CatalogItems { get; set; } = new List<CatalogItem>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<GiftCard> GiftCards { get; set; } = new List<GiftCard>();
    public ICollection<Discount> Discounts { get; set; } = new List<Discount>();
    
            
}