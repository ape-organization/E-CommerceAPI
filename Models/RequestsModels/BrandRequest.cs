namespace PharmacyAPI.Models.RequestsModels
{
    public class BrandRequest
    {
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;

        public IFormFile? Image { get; set; }
    }
}