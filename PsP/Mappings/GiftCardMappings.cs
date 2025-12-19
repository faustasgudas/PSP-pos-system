using PsP.Contracts.GiftCards;
using PsP.Models;

namespace PsP.Mappings;

public static class GiftCardMappings
{
    
    public static GiftCardResponse ToResponse(this GiftCard entity) =>
        new GiftCardResponse
        {
            GiftCardId = entity.GiftCardId,
            Code       = entity.Code,
            Balance    = entity.Balance,
            Status     = entity.Status,
            ExpiresAt  = entity.ExpiresAt,
            IssuedAt   = entity.IssuedAt
        };

    
    public static GiftCard ToNewEntity(this CreateGiftCardRequest req) =>
        new GiftCard
        {
            Code       = req.Code,
            Balance    = req.Balance,
            ExpiresAt  = req.ExpiresAt,
            Status     = "Active",
        };
}