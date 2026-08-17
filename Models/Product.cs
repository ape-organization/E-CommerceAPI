namespace PharmacyAPI.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsDeleted { get; set; } = false;

        public ICollection<SubCategory> SubCategories { get; set; }
       = new List<SubCategory>();

        public ICollection<OrderItem> OrderItems { get; set; }
            = new List<OrderItem>();
    }
}
