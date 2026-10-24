using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Data;
using PharmacyAPI.Models;
using PharmacyAPI.Models.RequestsModels;
using PharmacyAPI.Models.Responses;
using System.Linq.Expressions;
using System.Net;
using System.Text;

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

        Task<PagedResponse<OrderDto>> GetOrders(
        int page = 1,
        int pageSize = 100,
        int? orderId = null,
        OrderStatus? status = null,
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
        Task<bool> CompleteOrder(
            int id,
            CancellationToken cancellationToken = default);


        //dashboard services
        Task<DashboardStatsDto> GetCurrentMonthStats(
        CancellationToken cancellationToken = default);

        Task<DashboardStatsDto> GetTotalStats(
            CancellationToken cancellationToken = default);
    }

    public class OrderService : IOrderService
    {
        private readonly PharmacyDbContext _context;
        private readonly SmsIntegrationService _smsService;
        private readonly IEmailService _emailService;

        public OrderService(PharmacyDbContext context, SmsIntegrationService smsService, IEmailService emailService)
        {
            _context = context;
            _smsService = smsService;
            _emailService = emailService;
        }
        public async Task<DashboardStatsDto> GetCurrentMonthStats(
       CancellationToken cancellationToken = default)
        {
            var now = DateTime.Now;

            var startDate = new DateTime(
                now.Year,
                now.Month,
                1);

            var endDate = startDate.AddMonths(1);

            var orders = _context.Orders
                .AsNoTracking()
                .Where(o =>
                    o.Status != OrderStatus.Cancelled &&
                    o.OrderDate >= startDate &&
                    o.OrderDate < endDate);

            var orderItems = _context.OrderItems
                .AsNoTracking()
                .Where(i =>
                    i.Order.Status != OrderStatus.Cancelled &&
                    i.Order.OrderDate >= startDate &&
                    i.Order.OrderDate < endDate);

            var ordersCount = await orders
                .CountAsync(cancellationToken);

            var sales = await orders
                .SumAsync(
                    o => o.TotalAmount,
                    cancellationToken);

            var gain = await orderItems
                .SumAsync(
                    i => (i.UnitPrice - i.ActualPrice) * i.Quantity,
                    cancellationToken);

            return new DashboardStatsDto
            {
                Orders = ordersCount,

                Sales = Math.Round(
                    sales,
                    2,
                    MidpointRounding.AwayFromZero),

                Gain = Math.Round(
                    gain,
                    2,
                    MidpointRounding.AwayFromZero)
            };
        }


        public async Task<DashboardStatsDto> GetTotalStats(
            CancellationToken cancellationToken = default)
        {
            var orders = _context.Orders
                .AsNoTracking()
                .Where(o =>
                    o.Status != OrderStatus.Cancelled);

            var orderItems = _context.OrderItems
                .AsNoTracking()
                .Where(i =>
                    i.Order.Status != OrderStatus.Cancelled);

            var ordersCount = await orders
                .CountAsync(cancellationToken);

            var sales = await orders
                .SumAsync(
                    o => o.TotalAmount,
                    cancellationToken);

            var gain = await orderItems
                .SumAsync(
                    i => (i.UnitPrice - i.ActualPrice) * i.Quantity,
                    cancellationToken);

            return new DashboardStatsDto
            {
                Orders = ordersCount,

                Sales = Math.Round(
                    sales,
                    2,
                    MidpointRounding.AwayFromZero),

                Gain = Math.Round(
                    gain,
                    2,
                    MidpointRounding.AwayFromZero)
            };
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

        public async Task<bool> SendMsg(string phone,string msg)
            {

            return await _smsService.SendMessageAsync(
                phone,
                msg,
                CancellationToken.None) != null;
        }


        public async Task<Order> CreateOrder(
            CreateOrderDto dto,
            CancellationToken cancellationToken = default)
        {
            // =====================================================
            // VALIDATE REQUEST
            // =====================================================

            if (dto.Client == null)
                throw new InvalidOperationException("معلومات العميل غير متوفره.");

            if (string.IsNullOrWhiteSpace(dto.Client.Name))
                throw new InvalidOperationException("اسم العميل مطلوب.");

            if (string.IsNullOrWhiteSpace(dto.Client.PhoneNumber))
                throw new InvalidOperationException("رقم العميل مطلوب.");

            if (string.IsNullOrWhiteSpace(dto.Client.Address))
                throw new InvalidOperationException("عنوان العميل مطلوب.");

            if (string.IsNullOrWhiteSpace(dto.Client.governorate))
                throw new InvalidOperationException("محافظة العميل مطلوب.");

            if (dto.OrderRequest?.Items == null ||
                dto.OrderRequest.Items.Count == 0)
            {
                throw new InvalidOperationException(
                    "الطلب يجب ان يحتوي علي الاقل علي منتج واحد.");
            }

            if (dto.OrderRequest.ClientTotal < 0)
            {
                throw new InvalidOperationException(
                    "إجمالي الطلب غير صحيح.");
            }

            // =====================================================
            // VALIDATE ITEM QUANTITIES
            // =====================================================

            foreach (var item in dto.OrderRequest.Items)
            {
                if (item.ProductId <= 0)
                    throw new InvalidOperationException("منتج غير متوفر.");

                if (item.Quantity <= 0)
                    throw new InvalidOperationException(
                        "الكميه يجب ان تكون اكبر من الصفر.");
            }

            // =====================================================
            // COMBINE DUPLICATE PRODUCTS
            // =====================================================

            var requestedItems = dto.OrderRequest.Items
                .GroupBy(x => x.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Quantity = g.Sum(x => x.Quantity)
                })
                .ToList();

            // =====================================================
            // LOAD CURRENT PRODUCTS AND PRICES
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

            if (products.Count != productIds.Count)
            {
                var missingProductId = productIds
                    .First(id => !products.ContainsKey(id));

                throw new KeyNotFoundException(
                    $"المنتج {missingProductId} غير موجود.");
            }

            // =====================================================
            // CALCULATE TOTAL USING DATABASE PRICES
            // =====================================================

            decimal total = 0;

            var orderItems = new List<OrderItem>();

            foreach (var requestedItem in requestedItems)
            {
                var product = products[requestedItem.ProductId];

                var originalPrice = product.Price;
                var discount = product.DiscountPercentage;

                decimal unitPrice;

                if (discount > 0)
                {
                    unitPrice = originalPrice -
                        (originalPrice * discount / 100m);
                }
                else
                {
                    unitPrice = originalPrice;
                }

                // Match the frontend's unit-price rounding.
                unitPrice = Math.Round(
                    unitPrice,
                    2,
                    MidpointRounding.AwayFromZero);

                var subtotal = unitPrice * requestedItem.Quantity;

                total += subtotal;

                orderItems.Add(new OrderItem
                {
                    ActualPrice = product.ActualPrice,
                    ProductId = product.Id,
                    Quantity = requestedItem.Quantity,
                    UnitPrice = unitPrice
                });
            }

            var currentTotal = Math.Round(
                total,
                2,
                MidpointRounding.AwayFromZero);

            var clientTotal = Math.Round(
                dto.OrderRequest.ClientTotal,
                2,
                MidpointRounding.AwayFromZero);

            // =====================================================
            // REJECT IF PRICE CHANGED
            // =====================================================

            if (clientTotal != currentTotal)
            {
                throw new InvalidOperationException("حدث تغير في الاسعار من فضلك راجع الاسعار مره اخري ");
            }

            // =====================================================
            // START TRANSACTION AFTER PRICE VALIDATION
            // =====================================================

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var phoneNumber = dto.Client.PhoneNumber.Trim();
                var now = DateTime.UtcNow;

                // =================================================
                // FIND OR CREATE CLIENT
                // =================================================

                var client = await _context.Clients
                    .FirstOrDefaultAsync(
                        x => x.PhoneNumber == phoneNumber,
                        cancellationToken);

                if (client == null)
                {
                    client = new Client
                    {
                        Name = dto.Client.Name.Trim(),
                        PhoneNumber = phoneNumber,
                        Address = dto.Client.Address.Trim(),
                        governorate = dto.Client.governorate.Trim(),
                        Email = string.IsNullOrWhiteSpace(dto.Client.Email)
                            ? null
                            : dto.Client.Email.Trim(),
                        CreatedAt = now
                    };

                    _context.Clients.Add(client);
                    await _context.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    client.Name = dto.Client.Name.Trim();
                    client.Address = dto.Client.Address.Trim();
                    client.governorate = dto.Client.governorate.Trim();

                    client.Email = string.IsNullOrWhiteSpace(dto.Client.Email)
                        ? null
                        : dto.Client.Email.Trim();

                    client.UpdatedAt = now;

                    await _context.SaveChangesAsync(cancellationToken);
                }

                // =================================================
                // CREATE ORDER
                // =================================================

                var order = new Order
                {
                    ClientId = client.Id,
                    OrderDate = now,
                    Status = OrderStatus.Pending,
                    TotalAmount = currentTotal,
                    Items = orderItems
                };

                _context.Orders.Add(order);

                await _context.SaveChangesAsync(cancellationToken);

                // =================================================
                // COMMIT
                // =================================================

                await transaction.CommitAsync(cancellationToken);


                //send email to admin
                try
                {
                    Email(client, products, order);
                }
                catch (Exception ex)
                {
                    // Log the exception or handle it as needed
                    Console.WriteLine($"Failed to send email: {ex.Message}");
                }



                return order;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }




        //       public async Task<Order> CreateOrder(
        //   CreateOrderDto dto,
        //   CancellationToken cancellationToken = default)
        //       {
        //           // =====================================================
        //           // VALIDATE REQUEST
        //           // =====================================================

        //           if (dto.Client == null)
        //           {
        //               throw new InvalidOperationException(
        //                   "معلومات العميل غير متوفره.");
        //           }

        //           if (string.IsNullOrWhiteSpace(dto.Client.Name))
        //           {
        //               throw new InvalidOperationException(
        //                   "اسم العميل مطلوب.");
        //           }

        //           if (string.IsNullOrWhiteSpace(dto.Client.PhoneNumber))
        //           {
        //               throw new InvalidOperationException(
        //                   "رقم العميل مطلوب.");
        //           }

        //           if (string.IsNullOrWhiteSpace(dto.Client.Address))
        //           {
        //               throw new InvalidOperationException(
        //                   "عنوان العميل مطلوب.");
        //           }
        //           if (string.IsNullOrWhiteSpace(dto.Client.governorate))
        //           {
        //               throw new InvalidOperationException(
        //                   "محافظة العميل مطلوب.");
        //           }

        //           if (dto.OrderRequest.Items == null || dto.OrderRequest.Items.Count == 0)
        //           {
        //               throw new InvalidOperationException(
        //                   "الطلب يجب ان يحتوي علي الاقل علي منتج واحد.");
        //           }


        //           // =====================================================
        //           // VALIDATE ITEM QUANTITIES
        //           // =====================================================

        //           foreach (var item in dto.OrderRequest.Items)
        //           {
        //               if (item.ProductId <= 0)
        //               {
        //                   throw new InvalidOperationException(
        //                       "منتج غير متوفر.");
        //               }

        //               if (item.Quantity <= 0)
        //               {
        //                   throw new InvalidOperationException(
        //                       "الكميه يجب ان تكون اكبر من الصفر.");
        //               }
        //           }


        //           // =====================================================
        //           // PREVENT DUPLICATE PRODUCTS
        //           // =====================================================
        //           //
        //           // If the same product is sent twice:
        //           //
        //           // product 10 -> quantity 2
        //           // product 10 -> quantity 3
        //           //
        //           // It becomes:
        //           //
        //           // product 10 -> quantity 5
        //           //
        //           // =====================================================

        //           var requestedItems = dto.OrderRequest.Items
        //               .GroupBy(x => x.ProductId)
        //               .Select(g => new
        //               {
        //                   ProductId = g.Key,
        //                   Quantity = g.Sum(x => x.Quantity)
        //               })
        //               .ToList();


        //           // =====================================================
        //           // GET PRODUCTS
        //           // =====================================================

        //           var productIds = requestedItems
        //               .Select(x => x.ProductId)
        //               .ToList();

        //           var products = await _context.Products
        //               .AsNoTracking()
        //               .Where(p =>
        //                   productIds.Contains(p.Id) &&
        //                   !p.IsDeleted)
        //               .ToDictionaryAsync(
        //                   p => p.Id,
        //                   cancellationToken);


        //           // =====================================================
        //           // CHECK ALL PRODUCTS EXIST
        //           // =====================================================

        //           if (products.Count != productIds.Count)
        //           {
        //               var missingProductId = productIds
        //                   .First(id => !products.ContainsKey(id));

        //               throw new KeyNotFoundException(
        //                   $"المنتج {missingProductId} غير موجود.");
        //           }


        //           // =====================================================
        //           // FIND CLIENT
        //           // =====================================================

        //           var phoneNumber =
        //               dto.Client.PhoneNumber.Trim();

        //           var now =
        //               DateTime.UtcNow;

        //           var client = await _context.Clients
        //               .FirstOrDefaultAsync(
        //                   x => x.PhoneNumber == phoneNumber,
        //                   cancellationToken);


        //           // =====================================================
        //           // START TRANSACTION
        //           // =====================================================

        //           await using var transaction =
        //               await _context.Database.BeginTransactionAsync(
        //                   cancellationToken);

        //           try
        //           {
        //               // =================================================
        //               // CREATE CLIENT
        //               // =================================================

        //               if (client == null)
        //               {
        //                   client = new Client
        //                   {
        //                       Name = dto.Client.Name.Trim(),

        //                       PhoneNumber =
        //                           phoneNumber,

        //                       Address =
        //                           dto.Client.Address.Trim(),

        //                       governorate =

        //                           dto.Client.governorate.Trim(),
        //                       Email =
        //                           string.IsNullOrWhiteSpace(
        //                               dto.Client.Email)
        //                               ? null
        //                               : dto.Client.Email.Trim(),

        //                       CreatedAt = now
        //                   };

        //                   _context.Clients.Add(client);

        //                   await _context.SaveChangesAsync(
        //                       cancellationToken);
        //               }
        //               else
        //               {
        //                   // =============================================
        //                   // UPDATE EXISTING CLIENT
        //                   // =============================================

        //                   client.Name =
        //                       dto.Client.Name.Trim();

        //                   client.Address =
        //                       dto.Client.Address.Trim();
        //                   client.governorate =
        //                      dto.Client.governorate.Trim();    

        //                   client.Email =
        //                       string.IsNullOrWhiteSpace(
        //                           dto.Client.Email)
        //                           ? null
        //                           : dto.Client.Email.Trim();

        //                   client.UpdatedAt = now;

        //                   await _context.SaveChangesAsync(
        //                       cancellationToken);
        //               }


        //               // =================================================
        //               // CREATE ORDER
        //               // =================================================

        //               var order = new Order
        //               {
        //                   ClientId =
        //                       client.Id,

        //                   OrderDate =
        //                       now,

        //                   Status =
        //                       OrderStatus.Pending,

        //                   TotalAmount =
        //                       0,

        //                   Items =
        //                       new List<OrderItem>()
        //               };


        //               decimal total = 0;


        //               // =================================================
        //               // CREATE ORDER ITEMS
        //               // =================================================

        //               foreach (var requestedItem in requestedItems)
        //               {
        //                   var product =
        //                       products[requestedItem.ProductId];


        //                   // =============================================
        //                   // GET ORIGINAL PRICE
        //                   // =============================================

        //                   var originalPrice =
        //                       product.Price;


        //                   // =============================================
        //                   // GET DISCOUNT
        //                   // =============================================

        //                   var discount =
        //                       product.DiscountPercentage;


        //                   // =============================================
        //                   // CALCULATE FINAL PRICE
        //                   // =============================================

        //                   decimal unitPrice;

        //                   if (discount > 0)
        //                   {
        //                       unitPrice =
        //                           originalPrice -
        //                           (
        //                               originalPrice *
        //                               discount /
        //                               100
        //                           );
        //                   }
        //                   else
        //                   {
        //                       unitPrice =
        //                           originalPrice;
        //                   }


        //                   // =============================================
        //                   // ROUND MONEY TO 2 DECIMAL PLACES
        //                   // =============================================

        //                   unitPrice =
        //                       Math.Round(
        //                           unitPrice,
        //                           2,
        //                           MidpointRounding.AwayFromZero);


        //                   // =============================================
        //                   // ITEM TOTAL
        //                   // =============================================

        //                   var subtotal =
        //                       unitPrice *
        //                       requestedItem.Quantity;


        //                   total += subtotal;


        //                   // =============================================
        //                   // ADD ORDER ITEM
        //                   // =============================================

        //                   order.Items.Add(
        //                       new OrderItem
        //                       {
        //                           ActualPrice=product.ActualPrice,
        //                           ProductId =
        //                               product.Id,

        //                           Quantity =
        //                               requestedItem.Quantity,

        //                           UnitPrice =
        //                               unitPrice
        //                       });
        //               }


        //               // =================================================
        //               // FINAL TOTAL
        //               // =================================================

        //               order.TotalAmount =
        //                   Math.Round(
        //                       total,
        //                       2,
        //                       MidpointRounding.AwayFromZero);
        //               if(order.TotalAmount != dto.OrderRequest.ClientTotal)
        //               {

        //                   throw new InvalidOperationException(
        //                  "حدث تغير في الاسعار برجاء مراجعه الطلب مره اخري  .");
        //               }


        //               // =================================================
        //               // SAVE ORDER
        //               // =================================================

        //               _context.Orders.Add(order);

        //               await _context.SaveChangesAsync(
        //                   cancellationToken);

        ////  SendMsg(client.PhoneNumber,"order sent");
        //               // =================================================
        //               // COMMIT
        //               // =================================================

        //               await transaction.CommitAsync(
        //                   cancellationToken);


        //               return order;
        //           }
        //           catch
        //           {
        //               await transaction.RollbackAsync(
        //                   cancellationToken);

        //               throw;
        //           }
        //       }



        // =====================================================
        // GET ALL ORDERS
        // =====================================================

        public async Task<PagedResponse<OrderDto>> GetOrders(
       int page = 1,
       int pageSize = 100,
       int? orderId = null,
       OrderStatus? status = null,
       CancellationToken cancellationToken = default)
        {
            page = Math.Max(page, 1);

            pageSize = Math.Clamp(
                pageSize,
                1,
                100);

            var query =
                _context.Orders
                    .AsNoTracking();

            // Order ID filter
            if (orderId.HasValue)
            {
                query = query.Where(
                    o => o.Id == orderId.Value
                );
            }

            // Status filter
            if (status.HasValue)
            {
                query = query.Where(
                    o => o.Status == status.Value
                );
            }

            var items =
                await query
                    .OrderByDescending(o => o.OrderDate)
                    .ThenByDescending(o => o.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize + 1)
                    .Select(OrderProjection())
                    .ToListAsync(cancellationToken);

            var hasMore =
                items.Count > pageSize;

            if (hasMore)
            {
                items.RemoveAt(items.Count - 1);
            }

            return new PagedResponse<OrderDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                HasMore = hasMore
            };
        }
        //public async Task<PagedResponse<OrderDto>> GetOrders(int page = 1, int pageSize = 30,
        //    CancellationToken cancellationToken = default) 
        //{
        //    page = Math.Max(page, 1);
        //    pageSize = Math.Clamp(pageSize, 1, 100); 
        //    var items = await _context.Orders.AsNoTracking().OrderByDescending(o => o.OrderDate)
        //        .Skip((page - 1) * pageSize).Take(pageSize + 1).Select(OrderProjection())
        //        .ToListAsync(cancellationToken); 
        //    var hasMore = items.Count > pageSize; 
        //    if (hasMore) { items.RemoveAt(items.Count - 1); }
        //    return new PagedResponse<OrderDto> 
        //{ Items = items, Page = page, PageSize = pageSize, HasMore = hasMore }; }




        //public async Task<List<OrderDto>> GetOrders(
        //    CancellationToken cancellationToken = default)
        //{
        //    return await _context.Orders
        //        .AsNoTracking()
        //        .OrderByDescending(o => o.OrderDate)
        //        .Select(OrderProjection())
        //        .ToListAsync(cancellationToken);
        //}

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
        // COMPLETE ORDER
        // =====================================================

        public async Task<bool> CompleteOrder(
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
            var client=await _context.Clients
                .FirstOrDefaultAsync(
                    c => c.Id == order.ClientId,
                    cancellationToken);

            // Already cancelled
            if (order.Status == OrderStatus.Confirmed)
                return true;

            order.Status = OrderStatus.Confirmed;

            await _context.SaveChangesAsync(
                cancellationToken);

            SendMsg(client.PhoneNumber, " شكرا لتواصلكم مع Avera تم تاكيد طلبكم ");

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
                governorate = o.Client.governorate,

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



        public async void Email(Client client, Dictionary<int, Product> products, Order order)
        {

            // SEND ORDER EMAIL
            try
            {
                var itemsHtml = new StringBuilder();

                foreach (var item in order.Items)
                {
                    var product = products[item.ProductId];
                    var subtotal = item.UnitPrice * item.Quantity;

                   

                    itemsHtml.Append($@"
                <tr>
                    <td style='padding:14px 10px;border-bottom:1px solid #e2e8f0;'>
                        <strong>{WebUtility.HtmlEncode(product.NameAr)}</strong>
                        
                    </td>
                    <td style='padding:14px 6px;text-align:center;border-bottom:1px solid #e2e8f0;'>
                        {item.Quantity}
                    </td>
                    <td dir='ltr' style='padding:14px 6px;text-align:center;border-bottom:1px solid #e2e8f0;white-space:nowrap;'>
                        {item.UnitPrice:N2}
                    </td>
                    <td dir='ltr' style='padding:14px 10px;text-align:center;border-bottom:1px solid #e2e8f0;font-weight:bold;white-space:nowrap;'>
                        {subtotal:N2}
                    </td>
                </tr>");
                }

                var clientNameHtml =
                    WebUtility.HtmlEncode(client.Name ?? "");

                var clientPhoneHtml =
                    WebUtility.HtmlEncode(client.PhoneNumber ?? "");

                var clientAddressHtml =
                    WebUtility.HtmlEncode(client.Address ?? "");

                var clientEmailHtml =
                    WebUtility.HtmlEncode(
                        string.IsNullOrWhiteSpace(client.Email)
                            ? "غير مسجل"
                            : client.Email);

               

                var emailBody = $@"
<!DOCTYPE html>
<html lang='ar' dir='rtl'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>

<body dir='rtl' style='margin:0;padding:0;background:#f1f5f9;
    font-family:Tahoma,Arial,sans-serif;color:#1e293b;'>

<div style='max-width:760px;margin:30px auto;padding:0 12px;'>

    <!-- HEADER -->
    <div style='background:#172554;padding:30px 24px;
        border-radius:14px 14px 0 0;text-align:center;'>

        <h1 style='color:#ffffff;margin:0;font-size:27px;'>
            إشعار طلب جديد
        </h1>
    </div>

    <!-- CONTENT -->
    <div style='background:#ffffff;padding:26px 20px;'>

        <!-- ORDER SUMMARY -->
        <div style='background:#eff6ff;border:1px solid #dbeafe;
            border-radius:10px;padding:20px;margin-bottom:28px;'>

            <table role='presentation' width='100%' cellpadding='0' cellspacing='0'>
                <tr>
                    <td>
                        <div style='font-size:12px;color:#64748b;'>
                            رقم الطلب
                        </div>

                        <div dir='ltr' style='text-align:right;font-size:25px;
                            font-weight:bold;color:#1d4ed8;margin-top:8px;'>
                            #{order.Id}
                        </div>
                    </td>

                    <td style='text-align:left;vertical-align:top;'>
                        <span style='display:inline-block;background:#fef3c7;
                            color:#92400e;padding:8px 13px;border-radius:20px;
                            font-size:12px;font-weight:bold;'>
                            قيد الانتظار
                        </span>
                    </td>
                </tr>
            </table>
        </div>

        <!-- CLIENT DETAILS -->
        <h2 style='font-size:19px;color:#0f172a;margin:0 0 15px;'>
            بيانات العميل
        </h2>

        <table role='presentation' width='100%' cellpadding='0' cellspacing='0'
            style='border:1px solid #e2e8f0;border-radius:10px;
            margin-bottom:30px;font-size:14px;'>

            <tr>
                <td style='padding:14px;border-bottom:1px solid #e2e8f0;
                    width:115px;color:#64748b;'>
                    اسم العميل
                </td>
                <td style='padding:14px;border-bottom:1px solid #e2e8f0;font-weight:bold;'>
                    {clientNameHtml}
                </td>
            </tr>

            <tr>
                <td style='padding:14px;border-bottom:1px solid #e2e8f0;color:#64748b;'>
                    رقم الهاتف
                </td>
                <td dir='ltr' style='padding:14px;text-align:right;border-bottom:1px solid #e2e8f0;'>
                    {clientPhoneHtml}
                </td>
            </tr>

            <tr>
                <td style='padding:14px;border-bottom:1px solid #e2e8f0;color:#64748b;'>
                    البريد الإلكتروني
                </td>
                <td dir='ltr' style='padding:14px;text-align:right;border-bottom:1px solid #e2e8f0;word-break:break-word;'>
                    {clientEmailHtml}
                </td>
            </tr>

            <tr>
                <td style='padding:14px;color:#64748b;'>
                    عنوان العميل
                </td>
                <td style='padding:14px;line-height:1.9;'>
                    {clientAddressHtml}
                </td>
            </tr>
        </table>

        <!-- ORDER ITEMS -->
        <h2 style='font-size:19px;color:#0f172a;margin:0 0 15px;'>
            تفاصيل المنتجات
        </h2>

        <table width='100%' cellpadding='0' cellspacing='0'
            style='border-collapse:collapse;font-size:12px;'>

            <thead>
                <tr style='background:#f8fafc;'>
                    <th style='padding:13px 8px;text-align:right;color:#475569;border-bottom:2px solid #e2e8f0;'>
                        المنتج
                    </th>
                    <th style='padding:13px 5px;text-align:center;color:#475569;border-bottom:2px solid #e2e8f0;'>
                        الكمية
                    </th>
                    <th style='padding:13px 5px;text-align:center;color:#475569;border-bottom:2px solid #e2e8f0;'>
                        سعر الوحدة
                    </th>
                    <th style='padding:13px 5px;text-align:center;color:#475569;border-bottom:2px solid #e2e8f0;'>
                        الإجمالي
                    </th>
                </tr>
            </thead>

            <tbody>
                {itemsHtml}
            </tbody>
        </table>

       

        <!-- TOTAL -->
        <div style='margin-top:24px;background:#f8fafc;
            border-radius:10px;padding:22px;'>

            <table role='presentation' width='100%' cellpadding='0' cellspacing='0'>
                <tr>
                    <td style='font-size:15px;color:#475569;'>
                        إجمالي قيمة الطلب
                    </td>

                    <td dir='ltr' style='text-align:left;font-size:23px;
                        font-weight:bold;color:#1d4ed8;white-space:nowrap;'>
                        {order.TotalAmount:N2}
                    </td>
                </tr>
            </table>
        </div>

    </div>

    <!-- FOOTER -->
    <div style='background:#0f172a;padding:18px;
        text-align:center;border-radius:0 0 14px 14px;'>

        <p style='margin:0;color:#94a3b8;font-size:12px;line-height:2;'>
            يُرجى عدم الرد مباشرةً على هذه الرسالة.
        </p>
    </div>

</div>
</body>
</html>";

                await _emailService.SendEmailAsync(
                    "minaromany243@gmail.com",
                    $"إشعار طلب جديد رقم #{order.Id}",
                    emailBody);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Failed to send order email: {ex.Message}");
            }

        }






    }
}