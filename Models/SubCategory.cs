namespace PharmacyAPI.Models
{
    public class SubCategory
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsDeleted { get; set; } = false;

        // Foreign Key
        public int CategoryId { get; set; }

        // Navigation property
        public Category Category { get; set; } = null!;
         public ICollection<Product> Products { get; set; } = new List<Product>();

    }
}
