using System.Security.Claims;
using System.Security.Principal;

namespace COIS6980.AgendaLigera.Shared.Helpers
{
    public static class IdentityUserManager
    {
        public static string GetUserId(this IPrincipal principal)
        {
            var claimsIdentity = (ClaimsIdentity)principal.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            return claim?.Value;
        }
    }
}
