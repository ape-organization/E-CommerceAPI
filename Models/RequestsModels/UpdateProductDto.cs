namespace PharmacyAPI.Models.RequestsModels
{
    public class UpdateProductDto
    {
        public string NameEn { get; set; } = string.Empty;

        public string? DescriptionEn { get; set; }
        public string NameAr { get; set; } = string.Empty;

        public string? DescriptionAr { get; set; }

        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        public bool IsInStock { get; set; } = true;

        public decimal DiscountPercentage { get; set; } = 0;

        public IFormFile? Image { get; set; }

        public int BrandId { get; set; }

        public List<int> SubCategoryIds { get; set; } = new();
    }
}