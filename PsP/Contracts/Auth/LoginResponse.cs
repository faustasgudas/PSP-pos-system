namespace PsP.Contracts.Auth;

public class LoginResponse
{
    public string Token { get; set; } = null!;
    
    public string? BusinessType { get; set; }
}