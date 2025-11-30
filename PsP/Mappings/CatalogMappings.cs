using PsP.Contracts.Catalog;
using PsP.Models;

namespace PsP.Mappings;

public static class CatalogMappings
{
       // Entity -> Responses
    public static CatalogItemSummaryResponse ToSummaryResponse(this CatalogItem c) => new()
    {
        CatalogItemId = c.CatalogItemId,
        BusinessId    = c.BusinessId,
        Name          = c.Name,
        Code          = c.Code,
        Type          = c.Type,
        BasePrice     = c.BasePrice,
        Status        = c.Status,
        TaxClass      = c.TaxClass
    };

    public static CatalogItemDetailResponse ToDetailResponse(this CatalogItem c) => new()
    {
        CatalogItemId      = c.CatalogItemId,
        BusinessId         = c.BusinessId,
        Name               = c.Name,
        Code               = c.Code,
        Type               = c.Type,
        BasePrice          = c.BasePrice,
        Status             = c.Status,
        TaxClass           = c.TaxClass,
        DefaultDurationMin = c.DefaultDurationMin
    };

    // Request -> Entity
    public static CatalogItem ToNewEntity(this CreateCatalogItemRequest req, int businessId)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) throw new ArgumentException("Name required");
        if (string.IsNullOrWhiteSpace(req.Code)) throw new ArgumentException("Code required");
        if (string.IsNullOrWhiteSpace(req.Type)) throw new ArgumentException("Type required");
        if (string.IsNullOrWhiteSpace(req.TaxClass)) throw new ArgumentException("TaxClass required");

        return new CatalogItem
        {
            BusinessId         = businessId,
            Name               = req.Name.Trim(),
            Code               = req.Code.Trim().ToUpperInvariant(), // keep code normalized
            Type               = NormalizeType(req.Type),
            BasePrice          = req.BasePrice,
            Status             = NormalizeCatalogStatus(req.Status ?? "Active"),
            DefaultDurationMin = Math.Max(0, req.DefaultDurationMin),
            TaxClass           = req.TaxClass.Trim()
        };
    }

    // Apply partial update
    public static void ApplyUpdate(this UpdateCatalogItemRequest req, CatalogItem c)
    {
        if (!string.IsNullOrWhiteSpace(req.Name)) c.Name = req.Name.Trim();
        if (!string.IsNullOrWhiteSpace(req.Code)) c.Code = req.Code.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(req.Type)) c.Type = NormalizeType(req.Type!);
        if (req.BasePrice.HasValue) c.BasePrice = req.BasePrice.Value;
        if (!string.IsNullOrWhiteSpace(req.Status)) c.Status = NormalizeCatalogStatus(req.Status!);
        if (req.DefaultDurationMin.HasValue) c.DefaultDurationMin = Math.Max(0, req.DefaultDurationMin.Value);
        if (!string.IsNullOrWhiteSpace(req.TaxClass)) c.TaxClass = req.TaxClass!.Trim();
    }

    private static string NormalizeType(string type)
    {
        var t = type.Trim();
        return t.Equals("product", StringComparison.OrdinalIgnoreCase) ? "Product" :
               t.Equals("service", StringComparison.OrdinalIgnoreCase) ? "Service" :
               t;
    }

    private static string NormalizeCatalogStatus(string status)
    {
        var s = status.Trim();
        return s.Equals("draft", StringComparison.OrdinalIgnoreCase)   ? "Draft"   :
               s.Equals("active", StringComparison.OrdinalIgnoreCase)  ? "Active"  :
               s.Equals("archived", StringComparison.OrdinalIgnoreCase)? "Archived":
               s;
    }
}