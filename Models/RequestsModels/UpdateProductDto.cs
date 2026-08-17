namespace PharmacyAPI.Models.RequestsModels
{
    public class UpdateProductDto
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        public IFormFile? Image { get; set; }

        public List<int> SubCategoryIds { get; set; } = new();
    }
}
