using Microsoft.AspNetCore.Identity;

namespace PharmacyAPI.Models.RequestsModels
{


    public class UserDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Admin";
    }

    public class UpdateUserDto
    {
        public string name { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int RoleId { get; set; }
    }
    
}
