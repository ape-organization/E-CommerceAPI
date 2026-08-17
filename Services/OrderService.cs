
using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Data;
using PharmacyAPI.Models;
using PharmacyAPI.Models.RequestsModels;

namespace PharmacyAPI.Services
{
    public interface IOrderService
    {
        Task<OrderDto> CreateOrder(CreateOrderDto dto);

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

        public async Task<OrderDto> CreateOrder(
            CreateOrderDto dto)
        {
            if (dto.Items == null || !dto.Items.Any())
            {
                throw new InvalidOperationException(
                    "Order must contain at least one product.");
            }


            // ============================================
            // FIND OR CREATE CLIENT
            // ============================================

            var client = await _context.Clients
                .FirstOrDefaultAsync(c =>
                    c.PhoneNumber == dto.PhoneNumber);


            if (client == null)
            {
                client = new Client
                {
                    Name = dto.ClientName.Trim(),

                    PhoneNumber = dto.PhoneNumber.Trim(),

                    Email = dto.Email,

                    Address = dto.Address
                };

                _context.Clients.Add(client);

                await _context.SaveChangesAsync();
            }
            else
            {
                // Update latest customer information
                client.Name = dto.ClientName.Trim();

                client.Email = dto.Email;

                client.Address = dto.Address;
            }


            // ============================================
            // GET PRODUCTS
            // ============================================

            var productIds = dto.Items
                .Select(i => i.ProductId)
                .Distinct()
                .ToList();


            var products = await _context.Products
                .Where(p =>
                    productIds.Contains(p.Id) &&
                    !p.IsDeleted)
                .ToListAsync();


            if (products.Count != productIds.Count)
            {
                throw new KeyNotFoundException(
                    "One or more products were not found.");
            }


            // ============================================
            // CREATE ORDER
            // ============================================

            var order = new Order
            {
                ClientId = client.Id,

                OrderDate = DateTime.UtcNow,

                Status = OrderStatus.Pending,

                TotalAmount = 0
            };


            decimal totalAmount = 0;


            // ============================================
            // CREATE ORDER ITEMS
            // ============================================

            foreach (var item in dto.Items)
            {
                if (item.Quantity <= 0)
                {
                    throw new InvalidOperationException(
                        "Product quantity must be greater than zero.");
                }


                var product = products
                    .First(p => p.Id == item.ProductId);


                if (product.StockQuantity < item.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Not enough stock for product '{product.Name}'.");
                }


                var unitPrice = product.Price;

                var itemTotal =
                    unitPrice * item.Quantity;


                var orderItem = new OrderItem
                {
                    ProductId = product.Id,

                    Quantity = item.Quantity,

                    UnitPrice = unitPrice
                };


                order.Items.Add(orderItem);


                totalAmount += itemTotal;


                // Reduce stock
                product.StockQuantity -= item.Quantity;
            }


            order.TotalAmount = totalAmount;


            _context.Orders.Add(order);


            await _context.SaveChangesAsync();


            return await GetOrder(order.Id)
                ?? throw new InvalidOperationException(
                    "Order could not be created.");
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