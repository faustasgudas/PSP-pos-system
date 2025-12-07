using PsP.Models;

namespace PsP.Services.Interfaces.Auth;

public interface IJwtTokenService
{
    string GenerateToken(Business business, Employee employee);
}
