using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Data;
using PharmacyAPI.Models;
using PharmacyAPI.Models.RequestsModels;
using PharmacyAPI.Services;

namespace PharmacyAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(
            IOrderService orderService)
        {
            _orderService = orderService;
        }


        // ============================================
        // CREATE ORDER
        // POST: api/orders
        // ============================================

        [HttpPost]
        [AllowAnonymous]

        public async Task<IActionResult> CreateOrder(
       [FromBody] CreateOrderDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var order =
                    await _orderService.CreateOrder(dto);

                return CreatedAtAction(
                    nameof(GetOrder),
                    new { id = order.Id },
                    order);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
       
        
        
        // ============================================
        // GET ALL ORDERS
        // GET: api/orders
        // ============================================

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            var orders =
                await _orderService.GetOrders();

            return Ok(orders);
        }


        // ============================================
        // GET ORDER
        // GET: api/orders/5
        // ============================================

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var order =
                await _orderService.GetOrder(id);

            if (order == null)
            {
                return NotFound(new
                {
                    message = "Order not found."
                });
            }

            return Ok(order);
        }


        // ============================================
        // GET CLIENT ORDERS
        // GET: api/orders/client/5
        // ============================================

        [HttpGet("client/{clientId:int}")]
        public async Task<IActionResult> GetClientOrders(
            int clientId)
        {
            var orders =
                await _orderService.GetOrdersByClient(
                    clientId);

            return Ok(orders);
        }


        // ============================================
        // UPDATE STATUS
        // PUT: api/orders/5/status
        // ============================================

        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] string status)
        {
            try
            {
                var result =
                    await _orderService.UpdateOrderStatus(
                        id,
                        status);

                if (!result)
                {
                    return NotFound(new
                    {
                        message = "Order not found."
                    });
                }

                return Ok(new
                {
                    message =
                        "Order status updated successfully."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }


        // ============================================
        // CANCEL ORDER
        // PUT: api/orders/5/cancel
        // ============================================

        [HttpPut("{id:int}/cancel")]
        public async Task<IActionResult> CancelOrder(
            int id)
        {
            try
            {
                var result =
                    await _orderService.CancelOrder(id);

                if (!result)
                {
                    return NotFound(new
                    {
                        message = "Order not found."
                    });
                }

                return Ok(new
                {
                    message =
                        "Order cancelled successfully."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
    }
}