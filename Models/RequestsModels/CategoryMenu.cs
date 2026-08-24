namespace PharmacyAPI.Models.RequestsModels
{
    public class CategoryMenu
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public List<SubCategoryMenuDto> SubCategories { get; set; }
            = new List<SubCategoryMenuDto>();
    }

    public class SubCategoryMenuDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
