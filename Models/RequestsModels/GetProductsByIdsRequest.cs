namespace PharmacyAPI.Models.RequestsModels
{
    public class GetProductsByIdsRequest
    {
        public List<int> ProductIds { get; set; } = new();
    }
}
