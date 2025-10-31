using pm_api.Models;

namespace pm_api.Services
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}
