namespace PharmacyAPI.Models.RequestsModels
{
    public class CreateOrderDto
    {
        public string ClientName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Address { get; set; }

        public List<CreateOrderItemDto> Items { get; set; } = new();
    }
}
