using System.Security.Claims;

namespace API.Extensions
{
    public class ClaimsPrincipalExtensions
    {
        public static string GetUsername(this ClaimsPrincipal user){
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}