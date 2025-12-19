using PsP.Contracts.Businesses;
using PsP.Models;

namespace PsP.Mappings;

public static class BusinessMappings
{
  
    public static BusinessResponse ToResponse(this Business entity) =>
        new BusinessResponse
        {
            BusinessId       = entity.BusinessId,
            Name             = entity.Name,
            Address          = entity.Address,
            Phone            = entity.Phone,
            Email            = entity.Email,
            CountryCode      = entity.CountryCode,
            PriceIncludesTax = entity.PriceIncludesTax,
            BusinessStatus   = entity.BusinessStatus,
            BusinessType    = entity.BusinessType
        };

    
    public static Business ToNewEntity(this CreateBusinessRequest req) =>
        new Business
        {
            Name             = req.Name,
            Address          = req.Address,
            Phone            = req.Phone,
            Email            = req.Email,
            CountryCode      = req.CountryCode,
            PriceIncludesTax = req.PriceIncludesTax,
            BusinessStatus   = "Active",
            BusinessType = req.BusinessType
        };


    public static Business ToUpdatedEntity(this UpdateBusinessRequest req, int id) =>
        new Business
        {
            BusinessId       = id,
            Name             = req.Name,
            Address          = req.Address,
            Phone            = req.Phone,
            Email            = req.Email,
            CountryCode      = req.CountryCode,
            PriceIncludesTax = req.PriceIncludesTax,
            BusinessType = req.BusinessType
        };
}