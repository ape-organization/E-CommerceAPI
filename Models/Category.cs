namespace PharmacyAPI.Models
{
    public class Category
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public bool IsDeleted { get; set; } = false;

        public ICollection<SubCategory> SubCategories { get; set; }
            = new List<SubCategory>();
    }
}