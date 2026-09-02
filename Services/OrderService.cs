using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Data;
using PharmacyAPI.Models;
using PharmacyAPI.Models.RequestsModels;

namespace PharmacyAPI.Services
{
    public interface IOrderService
    {
        Task<Order> CreateOrder(
            CreateOrderDto dto,
            CancellationToken cancellationToken = default);

        Task<OrderDto?> GetOrder(
            int id,
            CancellationToken cancellationToken = default);

        Task<List<OrderDto>> GetOrders(
            CancellationToken cancellationToken = default);

        Task<List<OrderDto>> GetOrdersByClient(
            int clientId,
            CancellationToken cancellationToken = default);

        Task<bool> UpdateOrderStatus(
            int id,
            string status,
            CancellationToken cancellationToken = default);

        Task<bool> CancelOrder(
            int id,
            CancellationToken cancellationToken = default);
    }

    public class OrderService : IOrderService
    {
        private readonly PharmacyDbContext _context;

        public OrderService(PharmacyDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // CREATE ORDER
        // =====================================================

        public async Task<Order> CreateOrder(
            CreateOrderDto dto,
            CancellationToken cancellationToken = default)
        {
            if (dto.Client == null)
            {
                throw new InvalidOperationException(
                    "Client information is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.Client.PhoneNumber))
            {
                throw new InvalidOperationException(
                    "Client phone number is required.");
            }

            if (dto.Items == null || dto.Items.Count == 0)
            {
                throw new InvalidOperationException(
                    "Order must contain at least one item.");
            }

            // =================================================
            // VALIDATE ITEMS
            // =================================================

            foreach (var item in dto.Items)
            {
                if (item.Quantity <= 0)
                {
                    throw new InvalidOperationException(
                        "Quantity must be greater than zero.");
                }

                if (item.ProductId <= 0)
                {
                    throw new InvalidOperationException(
                        "Invalid product ID.");
                }
            }

            // =================================================
            // GET ALL PRODUCTS IN ONE QUERY
            //
            // Prevents N+1 queries.
            // =================================================

            var productIds = dto.Items
                .Select(x => x.ProductId)
                .Distinct()
                .ToList();

            var products = await _context.Products
                .AsNoTracking()
                .Where(p =>
                    productIds.Contains(p.Id) &&
                    !p.IsDeleted)
                .ToDictionaryAsync(
                    p => p.Id,
                    cancellationToken);

            // =================================================
            // CHECK ALL PRODUCTS EXIST
            // =================================================

            if (products.Count != productIds.Count)
            {
                var missingProductId = productIds
                    .First(id => !products.ContainsKey(id));

                throw new KeyNotFoundException(
                    $"Product {missingProductId} not found.");
            }

            // =================================================
            // FIND CLIENT
            // =================================================

            var phoneNumber = dto.Client.PhoneNumber.Trim();

            var client = await _context.Clients
                .FirstOrDefaultAsync(
                    x => x.PhoneNumber == phoneNumber,
                    cancellationToken);

            var now = DateTime.UtcNow;

            // =================================================
            // CREATE CLIENT IF NEEDED
            // =================================================

            if (client == null)
            {
                client = new Client
                {
                    Name = dto.Client.Name?.Trim(),
                    PhoneNumber = phoneNumber,
                    Address = dto.Client.Address?.Trim(),
                    Email = string.IsNullOrWhiteSpace(dto.Client.Email)
                        ? null
                        : dto.Client.Email.Trim(),
                    CreatedAt = now
                };

                _context.Clients.Add(client);

                // Client.Id is generated by SQL Server,
                // therefore save it before using ClientId.
                await _context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                client.Name = dto.Client.Name?.Trim();
                client.Address = dto.Client.Address?.Trim();

                client.Email =
                    string.IsNullOrWhiteSpace(dto.Client.Email)
                        ? null
                        : dto.Client.Email.Trim();

                client.UpdatedAt = now;
            }

            // =================================================
            // CREATE ORDER
            // =================================================

            var order = new Order
            {
                ClientId = client.Id,
                OrderDate = now,
                Status = OrderStatus.Confirmed,
                TotalAmount = 0,
                Items = new List<OrderItem>()
            };

            decimal total = 0;

            // =================================================
            // CREATE ORDER ITEMS
            // =================================================

            foreach (var itemDto in dto.Items)
            {
                var product = products[itemDto.ProductId];

                var unitPrice = product.Price;

                var subtotal =
                    unitPrice * itemDto.Quantity;

                total += subtotal;

                order.Items.Add(
                    new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = itemDto.Quantity,
                        UnitPrice = unitPrice
                    });
            }

            order.TotalAmount = total;

            // =================================================
            // SAVE ORDER
            // =================================================

            _context.Orders.Add(order);

            await _context.SaveChangesAsync(
                cancellationToken);

            return order;
        }

        // =====================================================
        // GET ALL ORDERS
        // =====================================================

        public async Task<List<OrderDto>> GetOrders(
            CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .AsNoTracking()
                .OrderByDescending(o => o.OrderDate)
                .Select(OrderProjection())
                .ToListAsync(cancellationToken);
        }

        // =====================================================
        // GET ORDER BY ID
        // =====================================================

        public async Task<OrderDto?> GetOrder(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .AsNoTracking()
                .Where(o => o.Id == id)
                .Select(OrderProjection())
                .FirstOrDefaultAsync(cancellationToken);
        }

        // =====================================================
        // GET ORDERS BY CLIENT
        // =====================================================

        public async Task<List<OrderDto>> GetOrdersByClient(
            int clientId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .AsNoTracking()
                .Where(o => o.ClientId == clientId)
                .OrderByDescending(o => o.OrderDate)
                .Select(OrderProjection())
                .ToListAsync(cancellationToken);
        }

        // =====================================================
        // UPDATE ORDER STATUS
        // =====================================================

        public async Task<bool> UpdateOrderStatus(
            int id,
            string status,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                throw new InvalidOperationException(
                    "Order status is required.");
            }

            if (!Enum.TryParse<OrderStatus>(
                    status.Trim(),
                    true,
                    out var orderStatus))
            {
                throw new InvalidOperationException(
                    "Invalid order status.");
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(
                    o => o.Id == id,
                    cancellationToken);

            if (order == null)
                return false;

            // Nothing to update
            if (order.Status == orderStatus)
                return true;

            order.Status = orderStatus;

            await _context.SaveChangesAsync(
                cancellationToken);

            return true;
        }

        // =====================================================
        // CANCEL ORDER
        // =====================================================

        public async Task<bool> CancelOrder(
            int id,
            CancellationToken cancellationToken = default)
        {
            // No Include(Items) needed.
            // We only change the order status.
            var order = await _context.Orders
                .FirstOrDefaultAsync(
                    o => o.Id == id,
                    cancellationToken);

            if (order == null)
                return false;

            // Already cancelled
            if (order.Status == OrderStatus.Cancelled)
                return true;

            order.Status = OrderStatus.Cancelled;

            await _context.SaveChangesAsync(
                cancellationToken);

            return true;
        }

        // =====================================================
        // ORDER PROJECTION
        //
        // EF translates this directly to SQL.
        // No Include() is required.
        // =====================================================

        private static Expression<Func<Order, OrderDto>>
            OrderProjection()
        {
            return o => new OrderDto
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

                Items = o.Items
                    .Select(i => new OrderItemDto
                    {
                        Id = i.Id,

                        ProductId = i.ProductId,

                        ProductName = i.Product.NameEn,

                        ImageUrl = i.Product.ImageUrl,

                        Quantity = i.Quantity,

                        UnitPrice = i.UnitPrice,

                        TotalPrice =
                            i.UnitPrice * i.Quantity
                    })
                    .ToList()
            };
        }
    }
}