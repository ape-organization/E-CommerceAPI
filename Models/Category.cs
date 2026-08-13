using System.Collections.Generic;

namespace PharmacyAPI.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
         public bool IsDeleted { get; set; }=false;
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
