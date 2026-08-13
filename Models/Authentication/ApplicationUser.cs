using Microsoft.AspNetCore.Identity;

namespace PharmacyAPI.Models.Authentication
{
    public class ApplicationUser : IdentityUser
    {
        public string? position { get; set; }
        public string? Name { get; set; }
        public string? RefreshToken { get; set; }
        public bool IsActive { get; set; } = false;

        public DateTime RefreshTokenExpiryTime { get; set; }
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public Cart? Cart { get; set; }
    }
}
