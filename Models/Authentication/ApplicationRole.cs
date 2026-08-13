using Microsoft.AspNetCore.Identity;

namespace PharmacyAPI.Models.Authentication
{
    public class ApplicationRole : IdentityRole
    {
        public ApplicationRole() { }
        public ApplicationRole(string roleName) : base(roleName)
        {
        }
    }
}
