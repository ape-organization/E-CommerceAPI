namespace PharmacyAPI.Models
{
    public class Product
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        // =====================================================
        // PRICE & STOCK
        // =====================================================

        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        /// <summary>
        /// Indicates whether the product is currently in stock.
        /// </summary>
        public bool IsInStock { get; set; } = true;

        public string? ImageUrl { get; set; }

        public bool IsDeleted { get; set; } = false;


        // =====================================================
        // DISCOUNT
        // =====================================================

        /// <summary>
        /// Discount percentage.
        /// Example: 20 means 20%
        /// </summary>
        public decimal DiscountPercentage { get; set; } = 0;


        /// <summary>
        /// Indicates whether the product currently has a discount.
        /// </summary>
        public bool HasDiscount =>
            DiscountPercentage > 0;


        /// <summary>
        /// Final price after applying the discount.
        /// This is calculated and NOT stored in the database.
        /// </summary>
        public decimal DiscountedPrice =>
            DiscountPercentage > 0
                ? Price - (Price * DiscountPercentage / 100)
                : Price;


        // =====================================================
        // BRAND
        // =====================================================

        public int BrandId { get; set; }

        public Brand? Brand { get; set; }


        // =====================================================
        // SUBCATEGORIES
        // =====================================================

        public ICollection<SubCategory> SubCategories { get; set; }
            = new List<SubCategory>();


        // =====================================================
        // ORDER ITEMS
        // =====================================================

        public ICollection<OrderItem> OrderItems { get; set; }
            = new List<OrderItem>();
    }
}