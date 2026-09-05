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

        //public async Task<Order> CreateOrder(
        //    CreateOrderDto dto,
        //    CancellationToken cancellationToken = default)
        //{
        //    if (dto.Client == null)
        //    {
        //        throw new InvalidOperationException(
        //            "معلومات العميل غير متوفره.");
        //    }

        //    if (string.IsNullOrWhiteSpace(dto.Client.PhoneNumber))
        //    {
        //        throw new InvalidOperationException(
        //            "رقم العميل مطلوب");
        //    }

        //    if (dto.Items == null || dto.Items.Count == 0)
        //    {
        //        throw new InvalidOperationException(
        //            "الطلب يجب ان يحتوي علي الاقل علي منتج واحد");
        //    }

        //    // =================================================
        //    // VALIDATE ITEMS
        //    // =================================================

        //    foreach (var item in dto.Items)
        //    {
        //        if (item.Quantity <= 0)
        //        {
        //            throw new InvalidOperationException(
        //                "الكميه يجب ان تكون اكبر من الصفر ");
        //        }

        //        if (item.ProductId <= 0)
        //        {
        //            throw new InvalidOperationException(
        //                "منتج غير متوفر.");
        //        }
        //    }

        //    // =================================================
        //    // GET ALL PRODUCTS IN ONE QUERY
        //    //
        //    // Prevents N+1 queries.
        //    // =================================================

        //    var productIds = dto.Items
        //        .Select(x => x.ProductId)
        //        .Distinct()
        //        .ToList();

        //    var products = await _context.Products
        //        .AsNoTracking()
        //        .Where(p =>
        //            productIds.Contains(p.Id) &&
        //            !p.IsDeleted)
        //        .ToDictionaryAsync(
        //            p => p.Id,
        //            cancellationToken);

        //    // =================================================
        //    // CHECK ALL PRODUCTS EXIST
        //    // =================================================

        //    if (products.Count != productIds.Count)
        //    {
        //        var missingProductId = productIds
        //            .First(id => !products.ContainsKey(id));

        //        throw new KeyNotFoundException(
        //            $"المنتج {missingProductId} غير موجود.");
        //    }

        //    // =================================================
        //    // FIND CLIENT
        //    // =================================================

        //    var phoneNumber = dto.Client.PhoneNumber.Trim();

        //    var client = await _context.Clients
        //        .FirstOrDefaultAsync(
        //            x => x.PhoneNumber == phoneNumber,
        //            cancellationToken);

        //    var now = DateTime.UtcNow;

        //    // =================================================
        //    // CREATE CLIENT IF NEEDED
        //    // =================================================

        //    if (client == null)
        //    {
        //        client = new Client
        //        {
        //            Name = dto.Client.Name?.Trim(),
        //            PhoneNumber = phoneNumber,
        //            Address = dto.Client.Address?.Trim(),
        //            Email = string.IsNullOrWhiteSpace(dto.Client.Email)
        //                ? null
        //                : dto.Client.Email.Trim(),
        //            CreatedAt = now
        //        };

        //        _context.Clients.Add(client);

        //        // Client.Id is generated by SQL Server,
        //        // therefore save it before using ClientId.
        //        await _context.SaveChangesAsync(cancellationToken);
        //    }
        //    else
        //    {
        //        client.Name = dto.Client.Name?.Trim();
        //        client.Address = dto.Client.Address?.Trim();

        //        client.Email =
        //            string.IsNullOrWhiteSpace(dto.Client.Email)
        //                ? null
        //                : dto.Client.Email.Trim();

        //        client.UpdatedAt = now;
        //    }

        //    // =================================================
        //    // CREATE ORDER
        //    // =================================================

        //    var order = new Order
        //    {
        //        ClientId = client.Id,
        //        OrderDate = now,
        //        Status = OrderStatus.Confirmed,
        //        TotalAmount = 0,
        //        Items = new List<OrderItem>()
        //    };

        //    decimal total = 0;

        //    // =================================================
        //    // CREATE ORDER ITEMS
        //    // =================================================

        //    foreach (var itemDto in dto.Items)
        //    {
        //        var product = products[itemDto.ProductId];

        //        var unitPrice = product.Price;

        //        var subtotal =
        //            unitPrice * itemDto.Quantity;

        //        total += subtotal;

        //        order.Items.Add(
        //            new OrderItem
        //            {
        //                ProductId = product.Id,
        //                Quantity = itemDto.Quantity,
        //                UnitPrice = unitPrice
        //            });
        //    }

        //    order.TotalAmount = total;

        //    // =================================================
        //    // SAVE ORDER
        //    // =================================================

        //    _context.Orders.Add(order);

        //    await _context.SaveChangesAsync(
        //        cancellationToken);

        //    return order;
        //}

      
        
public async Task<Order> CreateOrder(
    CreateOrderDto dto,
    CancellationToken cancellationToken = default)
        {
            // =====================================================
            // VALIDATE REQUEST
            // =====================================================

            if (dto.Client == null)
            {
                throw new InvalidOperationException(
                    "معلومات العميل غير متوفره.");
            }

            if (string.IsNullOrWhiteSpace(dto.Client.Name))
            {
                throw new InvalidOperationException(
                    "اسم العميل مطلوب.");
            }

            if (string.IsNullOrWhiteSpace(dto.Client.PhoneNumber))
            {
                throw new InvalidOperationException(
                    "رقم العميل مطلوب.");
            }

            if (string.IsNullOrWhiteSpace(dto.Client.Address))
            {
                throw new InvalidOperationException(
                    "عنوان العميل مطلوب.");
            }

            if (dto.Items == null || dto.Items.Count == 0)
            {
                throw new InvalidOperationException(
                    "الطلب يجب ان يحتوي علي الاقل علي منتج واحد.");
            }


            // =====================================================
            // VALIDATE ITEM QUANTITIES
            // =====================================================

            foreach (var item in dto.Items)
            {
                if (item.ProductId <= 0)
                {
                    throw new InvalidOperationException(
                        "منتج غير متوفر.");
                }

                if (item.Quantity <= 0)
                {
                    throw new InvalidOperationException(
                        "الكميه يجب ان تكون اكبر من الصفر.");
                }
            }


            // =====================================================
            // PREVENT DUPLICATE PRODUCTS
            // =====================================================
            //
            // If the same product is sent twice:
            //
            // product 10 -> quantity 2
            // product 10 -> quantity 3
            //
            // It becomes:
            //
            // product 10 -> quantity 5
            //
            // =====================================================

            var requestedItems = dto.Items
                .GroupBy(x => x.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Quantity = g.Sum(x => x.Quantity)
                })
                .ToList();


            // =====================================================
            // GET PRODUCTS
            // =====================================================

            var productIds = requestedItems
                .Select(x => x.ProductId)
                .ToList();

            var products = await _context.Products
                .AsNoTracking()
                .Where(p =>
                    productIds.Contains(p.Id) &&
                    !p.IsDeleted)
                .ToDictionaryAsync(
                    p => p.Id,
                    cancellationToken);


            // =====================================================
            // CHECK ALL PRODUCTS EXIST
            // =====================================================

            if (products.Count != productIds.Count)
            {
                var missingProductId = productIds
                    .First(id => !products.ContainsKey(id));

                throw new KeyNotFoundException(
                    $"المنتج {missingProductId} غير موجود.");
            }


            // =====================================================
            // FIND CLIENT
            // =====================================================

            var phoneNumber =
                dto.Client.PhoneNumber.Trim();

            var now =
                DateTime.UtcNow;

            var client = await _context.Clients
                .FirstOrDefaultAsync(
                    x => x.PhoneNumber == phoneNumber,
                    cancellationToken);


            // =====================================================
            // START TRANSACTION
            // =====================================================

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                // =================================================
                // CREATE CLIENT
                // =================================================

                if (client == null)
                {
                    client = new Client
                    {
                        Name = dto.Client.Name.Trim(),

                        PhoneNumber =
                            phoneNumber,

                        Address =
                            dto.Client.Address.Trim(),

                        Email =
                            string.IsNullOrWhiteSpace(
                                dto.Client.Email)
                                ? null
                                : dto.Client.Email.Trim(),

                        CreatedAt = now
                    };

                    _context.Clients.Add(client);

                    await _context.SaveChangesAsync(
                        cancellationToken);
                }
                else
                {
                    // =============================================
                    // UPDATE EXISTING CLIENT
                    // =============================================

                    client.Name =
                        dto.Client.Name.Trim();

                    client.Address =
                        dto.Client.Address.Trim();

                    client.Email =
                        string.IsNullOrWhiteSpace(
                            dto.Client.Email)
                            ? null
                            : dto.Client.Email.Trim();

                    client.UpdatedAt = now;

                    await _context.SaveChangesAsync(
                        cancellationToken);
                }


                // =================================================
                // CREATE ORDER
                // =================================================

                var order = new Order
                {
                    ClientId =
                        client.Id,

                    OrderDate =
                        now,

                    Status =
                        OrderStatus.Confirmed,

                    TotalAmount =
                        0,

                    Items =
                        new List<OrderItem>()
                };


                decimal total = 0;


                // =================================================
                // CREATE ORDER ITEMS
                // =================================================

                foreach (var requestedItem in requestedItems)
                {
                    var product =
                        products[requestedItem.ProductId];


                    // =============================================
                    // GET ORIGINAL PRICE
                    // =============================================

                    var originalPrice =
                        product.Price;


                    // =============================================
                    // GET DISCOUNT
                    // =============================================

                    var discount =
                        product.DiscountPercentage;


                    // =============================================
                    // CALCULATE FINAL PRICE
                    // =============================================

                    decimal unitPrice;

                    if (discount > 0)
                    {
                        unitPrice =
                            originalPrice -
                            (
                                originalPrice *
                                discount /
                                100
                            );
                    }
                    else
                    {
                        unitPrice =
                            originalPrice;
                    }


                    // =============================================
                    // ROUND MONEY TO 2 DECIMAL PLACES
                    // =============================================

                    unitPrice =
                        Math.Round(
                            unitPrice,
                            2,
                            MidpointRounding.AwayFromZero);


                    // =============================================
                    // ITEM TOTAL
                    // =============================================

                    var subtotal =
                        unitPrice *
                        requestedItem.Quantity;


                    total += subtotal;


                    // =============================================
                    // ADD ORDER ITEM
                    // =============================================

                    order.Items.Add(
                        new OrderItem
                        {
                            ProductId =
                                product.Id,

                            Quantity =
                                requestedItem.Quantity,

                            UnitPrice =
                                unitPrice
                        });
                }


                // =================================================
                // FINAL TOTAL
                // =================================================

                order.TotalAmount =
                    Math.Round(
                        total,
                        2,
                        MidpointRounding.AwayFromZero);


                // =================================================
                // SAVE ORDER
                // =================================================

                _context.Orders.Add(order);

                await _context.SaveChangesAsync(
                    cancellationToken);


                // =================================================
                // COMMIT
                // =================================================

                await transaction.CommitAsync(
                    cancellationToken);


                return order;
            }
            catch
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                throw;
            }
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
                    "حاله المنتج غير متوفره");
            }

            if (!Enum.TryParse<OrderStatus>(
                    status.Trim(),
                    true,
                    out var orderStatus))
            {
                throw new InvalidOperationException(
                    "حاله المنتج ليست مدعومه");
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