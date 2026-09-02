namespace PharmacyAPI.Models.RequestsModels
{
    public class ProductDto
    {
        public int Id { get; set; }

        public string NameAr { get; set; } = string.Empty;

        public string? DescriptionAr { get; set; }
        public string NameEn { get; set; } = string.Empty;

        public string? DescriptionEn { get; set; }

        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        public bool IsInStock { get; set; } = true;

        public string? ImageUrl { get; set; }

        public IFormFile? Image { get; set; }

        public decimal DiscountPercentage { get; set; } = 0;

        public int BrandId { get; set; }

        public List<int> SubCategoryIds { get; set; } = new();
    }
}