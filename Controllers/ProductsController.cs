using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Data;
using PharmacyAPI.Models;
using PharmacyAPI.Models.RequestsModels;
using PharmacyAPI.Services;
using System.IO;

namespace PharmacyAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {

        private readonly PharmacyDbContext _context;
        private readonly IProductService _productService;

        public ProductsController( 
            IProductService product, PharmacyDbContext context
            )
        {
            _productService = product;
            _context = context;
           
        }

        // ============================================
        // GET ALL PRODUCTS
        // GET: api/products
        // ============================================

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _productService.GetProducts();

            return Ok(products);
        }


        // ============================================
        // GET PRODUCT BY ID
        // GET: api/products/5
        // ============================================

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _productService.GetProduct(id);

            if (product == null)
            {
                return NotFound(new
                {
                    message = "Product not found."
                });
            }

            return Ok(product);
        }


        // ============================================
        // CREATE PRODUCT
        // POST: api/products
        // Content-Type: multipart/form-data
        // ============================================

        [HttpPost]
        public async Task<IActionResult> CreateProduct(
            [FromForm] ProductDto dto)
        {
            try
            {
                var product =
                    await _productService.CreateProduct(dto);

                return CreatedAtAction(
                    nameof(GetProduct),
                    new { id = product.Id },
                    product);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
        }


        // ============================================
        // UPDATE PRODUCT
        // PUT: api/products/5
        // Content-Type: multipart/form-data
        // ============================================

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateProduct(
            int id,
            [FromForm] UpdateProductDto dto)
        {
            try
            {
                var updated =
                    await _productService.UpdateProduct(
                        id,
                        dto);

                if (!updated)
                {
                    return NotFound(new
                    {
                        message = "Product not found."
                    });
                }

                var product =
                    await _productService.GetProduct(id);

                return Ok(product);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
        }


        // ============================================
        // DELETE PRODUCT
        // DELETE: api/products/5
        // ============================================

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var deleted =
                await _productService.DeleteProduct(id);

            if (!deleted)
            {
                return NotFound(new
                {
                    message = "Product not found."
                });
            }

            return Ok(new
            {
                message = "Product deleted successfully."
            });
        }


        // ============================================
        // CHECK PRODUCT NAME
        // GET: api/products/check-name?name=Panadol
        // ============================================

        [HttpGet("check-name")]
        public async Task<IActionResult> CheckProductExists(
            [FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new
                {
                    message = "Product name is required."
                });
            }

            var exists =
                await _productService.CheckProductExists(name);

            return Ok(new
            {
                exists
            });
        }
    }
}