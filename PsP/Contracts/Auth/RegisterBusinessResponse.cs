namespace PsP.Contracts.Auth;

public class RegisterBusinessResponse
{
    public int BusinessId { get; set; }
    public int OwnerEmployeeId { get; set; }
    public string Token { get; set; } = null!;
    
    public string BusinessType { get; set; }
}