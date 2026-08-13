using System.Collections.Generic;

namespace PharmacyAPI.Models
{
    public class Role
    {
        public string Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Navigation property
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
