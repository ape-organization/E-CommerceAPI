using Microsoft.AspNetCore.Http;

namespace PharmacyAPI.Models.RequestsModels
{
    public class CreateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;

        public IFormFile? Image { get; set; }
    }
}