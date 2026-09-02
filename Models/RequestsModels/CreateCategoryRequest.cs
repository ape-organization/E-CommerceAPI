using Microsoft.AspNetCore.Http;

namespace PharmacyAPI.Models.RequestsModels
{
    public class CreateCategoryRequest
    {
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;

        public IFormFile? Image { get; set; }
    }
}