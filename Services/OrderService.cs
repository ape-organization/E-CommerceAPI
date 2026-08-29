
using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Data;
using PharmacyAPI.Models;
using PharmacyAPI.Models.RequestsModels;

namespace PharmacyAPI.Services
{
    public interface IOrderService
    {
        Task<Order> CreateOrder(CreateOrderDto dto);

        Task<OrderDto?> GetOrder(int id);

        Task<List<OrderDto>> GetOrders();

        Task<List<OrderDto>> GetOrdersByClient(int clientId);

        Task<bool> UpdateOrderStatus(
            int id,
            string status);

        Task<bool> CancelOrder(int id);
    }
    public class OrderService : IOrderService
    {
        private readonly PharmacyDbContext _context;

        public OrderService(PharmacyDbContext context)
        {
            _context = context;
        }


        // ============================================
        // CREATE ORDER
        // ============================================

        public async Task<Order> CreateOrder(CreateOrderDto dto)
        {
            if (dto.Client == null)
                throw new InvalidOperationException(
                    "Client information is required.");

            if (string.IsNullOrWhiteSpace(dto.Client.PhoneNumber))
                throw new InvalidOperationException(
                    "Client phone number is required.");

            if (dto.Items == null || !dto.Items.Any())
                throw new InvalidOperationException(
                    "Order must contain at least one item.");

            // Find client by phone
            var client = await _context.Clients
                .FirstOrDefaultAsync(x =>
                    x.PhoneNumber == dto.Client.PhoneNumber);

            // Create client if not found
            if (client == null)
            {
                client = new Client
                {
                    Name = dto.Client.Name,
                    PhoneNumber = dto.Client.PhoneNumber,
                    Address = dto.Client.Address,
                    Email = dto.Client.Email,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Clients.Add(client);

                // We need the Client.Id before creating the Order
                await _context.SaveChangesAsync();
            }
            else
            {
                // Update client information
                client.Name = dto.Client.Name;
                client.Address = dto.Client.Address;
                client.Email = dto.Client.Email;
                client.UpdatedAt = DateTime.UtcNow;
            }

            // Create Order
            var order = new Order
            {
                ClientId = client.Id,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Confirmed,
                TotalAmount = 0,

                // Important: let EF Core manage the relationship
                Items = new List<OrderItem>()
            };

            decimal total = 0;

            foreach (var itemDto in dto.Items)
            {
                if (itemDto.Quantity <= 0)
                {
                    throw new InvalidOperationException(
                        "Quantity must be greater than zero.");
                }

                var product = await _context.Products
                    .FirstOrDefaultAsync(x =>
                        x.Id == itemDto.ProductId &&
                        !x.IsDeleted);

                if (product == null)
                {
                    throw new KeyNotFoundException(
                        $"Product {itemDto.ProductId} not found.");
                }

                var unitPrice = product.Price;

                var subtotal = unitPrice * itemDto.Quantity;

                var orderItem = new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = itemDto.Quantity,
                    UnitPrice = unitPrice
                };

                // IMPORTANT:
                // Don't set OrderId manually.
                // Add the item to the Order's collection.
                order.Items.Add(orderItem);

                total += subtotal;
            }

            order.TotalAmount = total;

            // Add the complete object graph
            _context.Orders.Add(order);

            // EF will:
            // 1. Insert Order
            // 2. Get generated Order.Id
            // 3. Insert OrderItems with the correct OrderId
            await _context.SaveChangesAsync();

            return order;
        }
        // ============================================
        // GET ALL ORDERS
        // ============================================

        public async Task<List<OrderDto>> GetOrders()
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Client)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => MapOrder(o))
                .ToListAsync();
        }


        // ============================================
        // GET ORDER BY ID
        // ============================================

        public async Task<OrderDto?> GetOrder(int id)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Client)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Where(o => o.Id == id)
                .Select(o => MapOrder(o))
                .FirstOrDefaultAsync();
        }


        // ============================================
        // GET CLIENT ORDERS
        // ============================================

        public async Task<List<OrderDto>> GetOrdersByClient(
            int clientId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Client)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Where(o => o.ClientId == clientId)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => MapOrder(o))
                .ToListAsync();
        }


        // ============================================
        // UPDATE ORDER STATUS
        // ============================================

        public async Task<bool> UpdateOrderStatus(
            int id,
            string status)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id);


            if (order == null)
                return false;


            if (!Enum.TryParse<OrderStatus>(
                    status,
                    true,
                    out var orderStatus))
            {
                throw new InvalidOperationException(
                    "Invalid order status.");
            }


            order.Status = orderStatus;


            await _context.SaveChangesAsync();

            return true;
        }


        // ============================================
        // CANCEL ORDER
        // ============================================

        public async Task<bool> CancelOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);


            if (order == null)
                return false;


            if (order.Status == OrderStatus.Delivered)
            {
                throw new InvalidOperationException(
                    "Delivered orders cannot be cancelled.");
            }


            if (order.Status == OrderStatus.Cancelled)
                return true;


            // Return products to stock
            foreach (var item in order.Items)
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p =>
                        p.Id == item.ProductId);


                if (product != null)
                {
                    product.StockQuantity += item.Quantity;
                }
            }


            order.Status = OrderStatus.Cancelled;


            await _context.SaveChangesAsync();

            return true;
        }


        // ============================================
        // MAPPING
        // ============================================

        private static OrderDto MapOrder(Order o)
        {
            return new OrderDto
            {
                Id = o.Id,

                ClientId = o.ClientId,

                ClientName = o.Client.Name,

                PhoneNumber = o.Client.PhoneNumber,

                Email = o.Client.Email,

                Address = o.Client.Address,

                OrderDate = o.OrderDate,

                TotalAmount = o.TotalAmount,

                Status = o.Status.ToString(),

                Items = o.Items.Select(i => new OrderItemDto
                {
                    Id = i.Id,

                    ProductId = i.ProductId,

                    ProductName = i.Product.Name,

                    ImageUrl = i.Product.ImageUrl,

                    Quantity = i.Quantity,

                    UnitPrice = i.UnitPrice,

                    TotalPrice =
                        i.UnitPrice * i.Quantity

                }).ToList()
            };
        }
    }
}