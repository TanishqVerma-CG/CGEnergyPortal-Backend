using BP.Models;

namespace BP.IService
{
    public interface IJwtService
    {
        LoginResponse GenerateToken(User user, string role);
    }
}
