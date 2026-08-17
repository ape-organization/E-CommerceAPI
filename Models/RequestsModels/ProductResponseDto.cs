namespace PharmacyAPI.Models.RequestsModels
{
    public class ProductResponseDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        public string? ImageUrl { get; set; }

        public List<SubCategoryResponseDto> SubCategories { get; set; }
            = new();
    }

    public class SubCategoryResponseDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;
    }
}
