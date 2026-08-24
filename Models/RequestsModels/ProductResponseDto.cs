namespace PharmacyAPI.Models.RequestsModels
{
    public class ProductResponseDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Price { get; set; }
        public bool IsInStock { get; set; } = true;

        public decimal DiscountPercentage { get; set; }

        public int StockQuantity { get; set; }

        public string? ImageUrl { get; set; }

        // =========================
        // BRAND
        // =========================

        public int? BrandId { get; set; }

        public BrandResponseDto? Brand { get; set; }

        // =========================
        // SUBCATEGORIES
        // =========================
        public int? CategoryId { get; set; }
        public List<SubCategoryResponseDto> SubCategories { get; set; }
            = new();
    }


    // =========================================================
    // BRAND RESPONSE
    // =========================================================

    public class BrandResponseDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }
    }


    // =========================================================
    // SUBCATEGORY RESPONSE
    // =========================================================

    public class SubCategoryResponseDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;
    }
}