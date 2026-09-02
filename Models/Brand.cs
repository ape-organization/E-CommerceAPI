namespace PharmacyAPI.Models
{
    public class Brand
    {
        public int Id { get; set; }

        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public bool IsDeleted { get; set; } = false;


        // =====================================================
        // RELATIONSHIP
        // One Brand -> Many Products
        // =====================================================

        public ICollection<Product> Products { get; set; }
            = new List<Product>();
    }
}