using PsP.Models;

namespace PsP.Contracts.Businesses;

public class BusinessResponse
{
    public int BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public bool PriceIncludesTax { get; set; }
    public string BusinessStatus { get; set; } = string.Empty;

    public static BusinessResponse FromEntity(Business entity)
    {
        return new BusinessResponse
        {
            BusinessId = entity.BusinessId,
            Name = entity.Name,
            Address = entity.Address,
            Phone = entity.Phone,
            Email = entity.Email,
            CountryCode = entity.CountryCode,
            PriceIncludesTax = entity.PriceIncludesTax,
            BusinessStatus = entity.BusinessStatus
        };
    }
}