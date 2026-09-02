namespace PharmacyAPI.Models.RequestsModels
{
    public class SubCategoryDto
    {
        public int Id { get; set; }
        public string NameEn { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;

        public int CategoryId { get; set; }
        public string CategoryNameEn { get; set; } = string.Empty;
        public string CategoryNameAr { get; set; } = string.Empty;

        public List<int> ProductIds { get; set; } = new();
    }
}
