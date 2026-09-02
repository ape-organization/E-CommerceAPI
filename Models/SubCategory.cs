namespace PharmacyAPI.Models
{
    public class SubCategory
    {
        public int Id { get; set; }

        public string NameEn { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;


        public bool IsDeleted { get; set; } = false;

        // Foreign Key
        public int CategoryId { get; set; }

        // Navigation property
        public Category Category { get; set; } = null!;
         public ICollection<Product> Products { get; set; } = new List<Product>();

    }
}
