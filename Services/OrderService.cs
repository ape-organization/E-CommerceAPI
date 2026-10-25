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

        // Dashboard
        Task<DashboardStatsDto> GetCurrentMonthStats(
            CancellationToken cancellationToken = default);

        Task<DashboardStatsDto> GetTotalStats(
            CancellationToken cancellationToken = default);
    }

    public class OrderService : IOrderService
    {
        private const int MaxPageSize = 100;

        private const string DefaultTimeZone = "Africa/Cairo";

        private const string OrderConfirmedSms =
            " شكرا لتواصلكم مع Avera تم تاكيد طلبكم ";

        private readonly PharmacyDbContext _context;
        private readonly SmsIntegrationService _smsService;
        private readonly IEmailService _emailService;
        private readonly ILogger<OrderService> _logger;
        private readonly string? _adminNotificationEmail;
        private readonly TimeZoneInfo _timeZone;

        public OrderService(
            PharmacyDbContext context,
            SmsIntegrationService smsService,
            IEmailService emailService,
            ILogger<OrderService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _smsService = smsService;
            _emailService = emailService;
            _logger = logger;

            _adminNotificationEmail =
                configuration["Orders:AdminNotificationEmail"];

            // IANA ids (e.g. "Africa/Cairo") work on both Linux and Windows.
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(
                configuration["Orders:TimeZone"] ?? DefaultTimeZone);
        }

        // =====================================================
        // DASHBOARD
        // =====================================================

        public Task<DashboardStatsDto> GetCurrentMonthStats(
            CancellationToken cancellationToken = default)
        {
            // Month boundaries are local to the business time zone,
            // but OrderDate is stored in UTC, so convert them back.
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                _timeZone);

            var localStart = new DateTime(
                localNow.Year,
                localNow.Month,
                1,
                0,
                0,
                0,
                DateTimeKind.Unspecified);

            var startDate = TimeZoneInfo.ConvertTimeToUtc(localStart, _timeZone);
            var endDate = TimeZoneInfo.ConvertTimeToUtc(localStart.AddMonths(1), _timeZone);

            return GetStats(
                o => o.OrderDate >= startDate && o.OrderDate < endDate,
                cancellationToken);
        }

        public Task<DashboardStatsDto> GetTotalStats(
            CancellationToken cancellationToken = default)
        {
            return GetStats(null, cancellationToken);
        }

        private async Task<DashboardStatsDto> GetStats(
            Expression<Func<Order, bool>>? filter,
            CancellationToken cancellationToken)
        {
            var orders = _context.Orders
                .AsNoTracking()
                .Where(o => o.Status != OrderStatus.Cancelled);

            if (filter != null)
                orders = orders.Where(filter);

            var ordersCount = await orders.CountAsync(cancellationToken);

            var sales = await orders.SumAsync(
                o => o.TotalAmount,
                cancellationToken);

            var gain = await orders
                .SelectMany(o => o.Items)
                .SumAsync(
                    i => (i.UnitPrice - i.ActualPrice) * i.Quantity,
                    cancellationToken);

            return new DashboardStatsDto
            {
                Orders = ordersCount,
                Sales = RoundMoney(sales),
                Gain = RoundMoney(gain)
            };
        }

        // =====================================================
        // CREATE ORDER
        // =====================================================

        public async Task<Order> CreateOrder(
            CreateOrderDto dto,
            CancellationToken cancellationToken = default)
        {
            ValidateCreateOrderRequest(dto);

            // Combine duplicate products into a single line.
            var requestedItems = dto.OrderRequest.Items
                .GroupBy(x => x.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Quantity = g.Sum(x => x.Quantity)
                })
                .ToList();

            var productIds = requestedItems
                .Select(x => x.ProductId)
                .ToList();

            var products = await _context.Products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            if (products.Count != productIds.Count)
            {
                var missingProductId = productIds
                    .First(id => !products.ContainsKey(id));

                throw new KeyNotFoundException(
                    $"المنتج {missingProductId} غير موجود.");
            }

            // =================================================
            // CALCULATE TOTAL USING DATABASE PRICES
            // =================================================

            decimal total = 0;
            var orderItems = new List<OrderItem>(requestedItems.Count);

            foreach (var requestedItem in requestedItems)
            {
                var product = products[requestedItem.ProductId];
                var unitPrice = CalculateUnitPrice(product);

                total += unitPrice * requestedItem.Quantity;

                orderItems.Add(new OrderItem
                {
                    ActualPrice = product.ActualPrice,
                    ProductId = product.Id,
                    Quantity = requestedItem.Quantity,
                    UnitPrice = unitPrice
                });
            }

            var currentTotal = RoundMoney(total);
            var clientTotal = RoundMoney(dto.OrderRequest.ClientTotal);

            // Reject if prices changed since the client loaded them.
            if (clientTotal != currentTotal)
            {
                throw new InvalidOperationException(
                    "حدث تغير في الاسعار من فضلك راجع الاسعار مره اخري ");
            }

            // =================================================
            // FIND OR CREATE CLIENT
            // =================================================

            var phoneNumber = dto.Client.PhoneNumber.Trim();
            var now = DateTime.UtcNow;

            var client = await _context.Clients
                .FirstOrDefaultAsync(
                    x => x.PhoneNumber == phoneNumber,
                    cancellationToken);

            if (client == null)
            {
                client = new Client
                {
                    PhoneNumber = phoneNumber,
                    CreatedAt = now
                };

                _context.Clients.Add(client);
            }
            else
            {
                client.UpdatedAt = now;
            }

            client.Name = dto.Client.Name.Trim();
            client.Address = dto.Client.Address!.Trim();
            client.governorate = dto.Client.governorate!.Trim();
            client.Email = NormalizeOptional(dto.Client.Email);

            // =================================================
            // CREATE ORDER
            //
            // Client and order are saved in a single
            // SaveChanges call, which EF runs in one transaction.
            // =================================================

            var order = new Order
            {
                Client = client,
                OrderDate = now,
                Status = OrderStatus.Pending,
                TotalAmount = currentTotal,
                Items = orderItems
            };

            _context.Orders.Add(order);

            await _context.SaveChangesAsync(cancellationToken);

            // Notify admin without delaying the response.
            _ = SendNewOrderEmailAsync(client, products, order);

            return order;
        }

        private static void ValidateCreateOrderRequest(CreateOrderDto dto)
        {
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
                throw new InvalidOperationException("إجمالي الطلب غير صحيح.");

            foreach (var item in dto.OrderRequest.Items)
            {
                if (item.ProductId <= 0)
                    throw new InvalidOperationException("منتج غير متوفر.");

                if (item.Quantity <= 0)
                    throw new InvalidOperationException(
                        "الكميه يجب ان تكون اكبر من الصفر.");
            }
        }

        private static decimal CalculateUnitPrice(Product product)
        {
            var originalPrice = product.Price;
            var discount = product.DiscountPercentage;

            var unitPrice = discount > 0
                ? originalPrice - (originalPrice * discount / 100m)
                : originalPrice;

            // Match the frontend's unit-price rounding.
            return RoundMoney(unitPrice);
        }

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
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var query = _context.Orders.AsNoTracking();

            if (orderId.HasValue)
                query = query.Where(o => o.Id == orderId.Value);

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            // Fetch one extra row to know whether another page exists.
            var items = await query
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize + 1)
                .Select(OrderProjection)
                .ToListAsync(cancellationToken);

            var hasMore = items.Count > pageSize;

            if (hasMore)
                items.RemoveAt(items.Count - 1);

            return new PagedResponse<OrderDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                HasMore = hasMore
            };
        }

        // =====================================================
        // GET ORDER BY ID
        // =====================================================

        public Task<OrderDto?> GetOrder(
            int id,
            CancellationToken cancellationToken = default)
        {
            return _context.Orders
                .AsNoTracking()
                .Where(o => o.Id == id)
                .Select(OrderProjection)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // =====================================================
        // GET ORDERS BY CLIENT
        // =====================================================

        public Task<List<OrderDto>> GetOrdersByClient(
            int clientId,
            CancellationToken cancellationToken = default)
        {
            return _context.Orders
                .AsNoTracking()
                .Where(o => o.ClientId == clientId)
                .OrderByDescending(o => o.OrderDate)
                .Select(OrderProjection)
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
                throw new InvalidOperationException("حاله المنتج غير متوفره");

            if (!Enum.TryParse<OrderStatus>(status.Trim(), true, out var orderStatus))
                throw new InvalidOperationException("حاله المنتج ليست مدعومه");

            var order = await FindOrderAsync(id, cancellationToken);

            if (order == null)
                return false;

            await SetStatusAsync(order, orderStatus, cancellationToken);

            return true;
        }

        // =====================================================
        // CANCEL ORDER
        // =====================================================

        public async Task<bool> CancelOrder(
            int id,
            CancellationToken cancellationToken = default)
        {
            var order = await FindOrderAsync(id, cancellationToken);

            if (order == null)
                return false;

            await SetStatusAsync(order, OrderStatus.Cancelled, cancellationToken);

            return true;
        }

        // =====================================================
        // COMPLETE ORDER
        // =====================================================

        public async Task<bool> CompleteOrder(
            int id,
            CancellationToken cancellationToken = default)
        {
            var order = await FindOrderAsync(id, cancellationToken);

            if (order == null)
                return false;

            // Already confirmed
            if (order.Status == OrderStatus.Confirmed)
                return true;

            await SetStatusAsync(order, OrderStatus.Confirmed, cancellationToken);

            var phoneNumber = await _context.Clients
                .AsNoTracking()
                .Where(c => c.Id == order.ClientId)
                .Select(c => c.PhoneNumber)
                .FirstOrDefaultAsync(cancellationToken);

            // Notify client without delaying the response.
            if (!string.IsNullOrWhiteSpace(phoneNumber))
                _ = SendSmsAsync(phoneNumber, OrderConfirmedSms);

            return true;
        }

        // =====================================================
        // HELPERS
        // =====================================================

        // No Include(Items) needed; only the order status changes.
        private Task<Order?> FindOrderAsync(
            int id,
            CancellationToken cancellationToken)
        {
            return _context.Orders.FirstOrDefaultAsync(
                o => o.Id == id,
                cancellationToken);
        }

        private async Task SetStatusAsync(
            Order order,
            OrderStatus status,
            CancellationToken cancellationToken)
        {
            if (order.Status == status)
                return;

            order.Status = status;

            await _context.SaveChangesAsync(cancellationToken);
        }

        private static decimal RoundMoney(decimal value) =>
            Math.Round(value, 2, MidpointRounding.AwayFromZero);

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        // =====================================================
        // ORDER PROJECTION
        //
        // EF translates this directly to SQL.
        // No Include() is required.
        // =====================================================

        private static readonly Expression<Func<Order, OrderDto>> OrderProjection =
            o => new OrderDto
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
                        TotalPrice = i.UnitPrice * i.Quantity
                    })
                    .ToList()
            };

        // =====================================================
        // NOTIFICATIONS
        //
        // Fire-and-forget: failures are logged and never
        // affect the order operation that triggered them.
        // =====================================================

        private async Task SendSmsAsync(string phone, string message)
        {
            try
            {
                await _smsService.SendMessageAsync(
                    phone,
                    message,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to {Phone}.", phone);
            }
        }

        private async Task SendNewOrderEmailAsync(
            Client client,
            Dictionary<int, Product> products,
            Order order)
        {
            if (string.IsNullOrWhiteSpace(_adminNotificationEmail))
            {
                _logger.LogWarning(
                    "Orders:AdminNotificationEmail is not configured; " +
                    "skipping email for order {OrderId}.",
                    order.Id);

                return;
            }

            try
            {
                var emailBody = BuildNewOrderEmailBody(client, products, order);

                await _emailService.SendEmailAsync(
                    _adminNotificationEmail,
                    $"إشعار طلب جديد رقم #{order.Id}",
                    emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send order email for order {OrderId}.",
                    order.Id);
            }
        }

        private static string BuildNewOrderEmailBody(
            Client client,
            Dictionary<int, Product> products,
            Order order)
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

            var clientNameHtml = WebUtility.HtmlEncode(client.Name ?? "");
            var clientPhoneHtml = WebUtility.HtmlEncode(client.PhoneNumber ?? "");
            var clientAddressHtml = WebUtility.HtmlEncode(client.Address ?? "");

            var clientEmailHtml = WebUtility.HtmlEncode(
                string.IsNullOrWhiteSpace(client.Email)
                    ? "غير مسجل"
                    : client.Email);

            return $@"
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
        }
    }
}
