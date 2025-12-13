using PsP.Contracts.Businesses;
using PsP.Models;

namespace PsP.Mappings;

public static class BusinessMappings
{
    // Entity -> Response DTO
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

    // CreateBusinessRequest -> naujas Business entity
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

    // UpdateBusinessRequest -> "detached" Business su atnaujintom reikšmėm
    // (BusinessService.UpdateAsync vis tiek perkopijuoja laukus į existing entity)
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