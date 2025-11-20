using System.ComponentModel.DataAnnotations;

namespace PsP.Contracts.GiftCards;

public class CreateGiftCardRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int BusinessId { get; set; }  

    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Range(0, long.MaxValue)]
    public long Balance { get; set; }

    public DateTime? ExpiresAt { get; set; }
}