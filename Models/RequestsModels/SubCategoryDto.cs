namespace PharmacyAPI.Models.RequestsModels
{
    public class SubCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;

        public List<int> ProductIds { get; set; } = new();
    }
}
