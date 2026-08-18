namespace PharmacyAPI.Models.RequestsModels
{
    public class CreateOrderDto
    {
        public ClientDto Client { get; set; } = new();

        public List<CreateOrderItemDto> Items { get; set; }
            = new();
    }

    public class ClientDto
    {
        public string Name { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string? Address { get; set; }

        public string? Email { get; set; }
    }

    public class CreateOrderItemDto
    {
        public int ProductId { get; set; }

        public int Quantity { get; set; }
    }
}