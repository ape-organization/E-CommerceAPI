using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyAPI.Data;
using PharmacyAPI.Models;
using PharmacyAPI.Models.RequestsModels;
using PharmacyAPI.Services;

namespace PharmacyAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly PharmacyDbContext _context;
        private readonly IProductService _productService;

        public ProductsController(
            IProductService product,
            PharmacyDbContext context)
        {
            _productService = product;
            _context = context;
        }


        // =====================================================
        // GET ALL PRODUCTS
        // GET: api/products
        // =====================================================

       
      [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetProducts(
    [FromQuery] int? categoryId,
    [FromQuery] int? subCategoryId,
    [FromQuery] int? brandId,
    [FromQuery] bool? offers)
        {
            var products = await _productService.GetProducts(
                categoryId,
                subCategoryId,
                brandId,
                offers
            );

            return Ok(products);
        }


        // =====================================================
        // GET ALL DISCOUNTED PRODUCTS
        // GET: api/products/discounted
        // =====================================================

        [HttpGet("discounted")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDiscountedProducts()
        {
            var products =
                await _productService.GetDiscountedProducts();

            return Ok(products);
        }


        // =====================================================
        // GET PRODUCT BY ID
        // GET: api/products/5
        // =====================================================

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product =
                await _productService.GetProduct(id);

            if (product == null)
            {
                return NotFound(new
                {
                    message = "Product not found."
                });
            }

            return Ok(product);
        }


        // =====================================================
        // CREATE PRODUCT
        // POST: api/products
        // Content-Type: multipart/form-data
        // =====================================================

        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateProduct(
            [FromForm] ProductDto dto)
        {
            try
            {
                var product =
                    await _productService.CreateProduct(dto);

                return CreatedAtAction(
                    nameof(GetProduct),
                    new
                    {
                        id = product.Id
                    },
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


        // =====================================================
        // UPDATE PRODUCT
        // PUT: api/products/5
        // Content-Type: multipart/form-data
        // =====================================================

        [HttpPut("{id:int}")]
        [Authorize]
        [Consumes("multipart/form-data")]
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


        // =====================================================
        // REMOVE DISCOUNT
        // PUT: api/products/5/remove-discount
        // =====================================================

        [HttpPut("{id:int}/remove-discount")]
        [Authorize]
        public async Task<IActionResult> RemoveDiscount(int id)
        {
            try
            {
                var result =
                    await _productService.RemoveDiscount(id);

                if (!result)
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
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
        }


        // =====================================================
        // DELETE PRODUCT
        // DELETE: api/products/5
        // =====================================================

        [HttpDelete("{id:int}")]
        [Authorize]
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


        // =====================================================
        // CHECK PRODUCT NAME
        // GET: api/products/check-name?name=Panadol
        // =====================================================

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