using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyAPI.Models.RequestsModels;
using PharmacyAPI.Models.Responses;
using PharmacyAPI.Services;

namespace PharmacyAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(
            IProductService productService)
        {
            _productService = productService;
        }
        //=================================================
        // get best seller product 
        //=================================================
        [AllowAnonymous]
        [HttpGet("best-sellers")]
        public async Task<ActionResult<List<ProductResponseDto>>> GetBestSellers(
    [FromQuery] int count = 10,
    CancellationToken cancellationToken = default)
        {
            var products = await _productService.GetBestSellerProducts(
                count,
                cancellationToken);

            return Ok(products);
        }

        // =====================================================
        // GET PRODUCTS
        // GET: api/products
        // =====================================================

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResponse<ProductResponseDto>>> GetProducts(
            [FromQuery] int page = 1,
            [FromQuery] int? categoryId = null,
            [FromQuery] int? subCategoryId = null,
            [FromQuery] int? brandId = null,
            [FromQuery] bool? offers = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _productService.GetProducts(
                page,
                categoryId,
                subCategoryId,
                brandId,
                offers,
                cancellationToken);

            return Ok(result);
        }


        // =====================================================
        // GET CART PRODUCTS
        // POST: api/products/cart
        // =====================================================

        [HttpPost("cart")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCartProducts(
            [FromBody] GetProductsByIdsRequest request,
            CancellationToken cancellationToken)
        {
            if (request?.ProductIds is null ||
                request.ProductIds.Count == 0)
            {
                return Ok(new List<ProductResponseDto>());
            }

            var products =
                await _productService.GetProductsByIds(
                    request.ProductIds,
                    cancellationToken);

            return Ok(products);
        }


        // =====================================================
        // GET DISCOUNTED PRODUCTS
        // GET: api/products/discounted
        // =====================================================

        [HttpGet("discounted")]
        [AllowAnonymous]
        public async Task<ActionResult<List<ProductResponseDto>>> GetDiscountedProducts(
            CancellationToken cancellationToken)
        {
            var products =
                await _productService.GetDiscountedProducts(
                    cancellationToken);

            return Ok(products);
        }


        // =====================================================
        // GET PRODUCT BY ID
        // GET: api/products/5
        // =====================================================

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProduct(
            int id,
            CancellationToken cancellationToken)
        {
            var product =
                await _productService.GetProduct(
                    id,
                    cancellationToken);

            if (product is null)
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
        // =====================================================

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateProduct(
            [FromForm] ProductDto dto,
            CancellationToken cancellationToken)
        {
            try
            {
                var product =
                    await _productService.CreateProduct(
                        dto,
                        cancellationToken);

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


        // =====================================================
        // UPDATE PRODUCT
        // PUT: api/products/5
        // =====================================================

        [HttpPut("{id:int}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateProduct(
            int id,
            [FromForm] UpdateProductDto dto,
            CancellationToken cancellationToken)
        {
            try
            {
                var updated =
                    await _productService.UpdateProduct(
                        id,
                        dto,
                        cancellationToken);

                if (!updated)
                {
                    return NotFound(new
                    {
                        message = "Product not found."
                    });
                }

                // Return 204.
                // No second SELECT is required.
                return NoContent();
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
        public async Task<IActionResult> RemoveDiscount(
            int id,
            CancellationToken cancellationToken)
        {
            var product =
                await _productService.RemoveDiscount(
                    id,
                    cancellationToken);

            if (product is null)
            {
                return NotFound(new
                {
                    message = "Product not found."
                });
            }

            return Ok(product);
        }


        // =====================================================
        // DELETE PRODUCT
        // DELETE: api/products/5
        // =====================================================

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteProduct(
            int id,
            CancellationToken cancellationToken)
        {
            var deleted =
                await _productService.DeleteProduct(
                    id,
                    cancellationToken);

            if (!deleted)
            {
                return NotFound(new
                {
                    message = "Product not found."
                });
            }

            return NoContent();
        }


        // =====================================================
        // SEARCH PRODUCTS BY NAME
        // GET: api/products/by-name?name=Panadol
        // =====================================================

        [HttpGet("by-name")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductsByName(
            [FromQuery] string name,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new
                {
                    message = "Product name is required."
                });
            }

            var products =
                await _productService.GetProductsByName(
                    name,
                    cancellationToken);

            return Ok(products);
        }


        // =====================================================
        // CHECK PRODUCT NAME
        // GET: api/products/check-name?name=Panadol
        // =====================================================

        [HttpGet("check-name")]
        public async Task<IActionResult> CheckProductExists(
            [FromQuery] string name,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new
                {
                    message = "Product name is required."
                });
            }

            var exists =
                await _productService.CheckProductExists(
                    name,
                    cancellationToken);

            return Ok(new
            {
                exists
            });
        }
    }
}