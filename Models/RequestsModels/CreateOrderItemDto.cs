namespace PharmacyAPI.Models.RequestsModels
{
    public class CreateOrderItemDto
    {
        public int ProductId { get; set; }

        public int Quantity { get; set; }
    }
}
