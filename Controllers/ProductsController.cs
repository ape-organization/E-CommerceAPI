using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Data;
using PharmacyAPI.Models;
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
        private readonly PharmacyDbContext _context;
        private readonly IProductService _productService;

        public ProductsController(
            IProductService product,
            PharmacyDbContext context)
        {
            _productService = product;
            _context = context;
        }
        //=============================
        // check products ids exists 
        //==============================
        [HttpPost("cart")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCartProducts(
    [FromBody] GetProductsByIdsRequest request)
        {
            if (request == null ||
                request.ProductIds == null ||
                request.ProductIds.Count == 0)
            {
                return Ok(new List<ProductResponseDto>());
            }

            var products =
                await _productService.GetProductsByIds(
                    request.ProductIds
                );

            return Ok(products);
        }

        // =====================================================
        // GET ALL PRODUCTS
        // GET: api/products
        // =====================================================

        [HttpGet]
        [AllowAnonymous]
        public async Task<PagedResponse<ProductResponseDto>> GetProducts(
    int page = 1,
    int? categoryId = null,
    int? subCategoryId = null,
    int? brandId = null,
    bool? offers = null)
        {
            try
            {
                const int pageSize = 100;

                if (page < 1)
                    page = 1;

                var query = _context.Products
                    .AsNoTracking()
                    .Where(p => !p.IsDeleted);

                // =========================
                // FILTERS
                // =========================

                if (categoryId.HasValue)
                {
                    query = query.Where(p =>
                        p.SubCategories.Any(sc =>
                            !sc.IsDeleted &&
                            sc.CategoryId == categoryId.Value));
                }

                if (subCategoryId.HasValue)
                {
                    query = query.Where(p =>
                        p.SubCategories.Any(sc =>
                            !sc.IsDeleted &&
                            sc.Id == subCategoryId.Value));
                }

                if (brandId.HasValue)
                {
                    query = query.Where(p =>
                        p.BrandId == brandId.Value);
                }

                if (offers == true)
                {
                    query = query.Where(p =>
                        p.DiscountPercentage > 0);
                }

                // =========================
                // ARE WE FILTERING?
                // =========================

                bool hasFilters =
                    categoryId.HasValue ||
                    subCategoryId.HasValue ||
                    brandId.HasValue ||
                    offers == true;

                // =========================
                // TOTAL
                // =========================

                var totalCount = await query.CountAsync();

                // =========================
                // PAGINATION
                //
                // NO FILTER = 100
                // FILTER = ALL
                // =========================

                int totalPages;

                if (hasFilters)
                {
                    totalPages = totalCount > 0 ? 1 : 0;
                }
                else
                {
                    totalPages =
                        (int)Math.Ceiling(
                            totalCount / (double)pageSize);

                    query = query
                        .OrderBy(p => p.Id)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize);
                }

                var products = await query
                    .OrderBy(p => p.Id)
                    .Select(p => new ProductResponseDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        DiscountPercentage = p.DiscountPercentage,
                        StockQuantity = p.StockQuantity,
                        IsInStock = p.IsInStock,
                        ImageUrl = p.ImageUrl,

                        BrandId = p.BrandId,

                        Brand = p.Brand == null
                            ? null
                            : new BrandResponseDto
                            {
                                Id = p.Brand.Id,
                                Name = p.Brand.Name,
                                ImageUrl = p.Brand.ImageUrl
                            },

                        CategoryId = p.SubCategories
                            .Where(sc => !sc.IsDeleted)
                            .Select(sc => (int?)sc.CategoryId)
                            .FirstOrDefault(),

                        SubCategories = p.SubCategories
                            .Where(sc => !sc.IsDeleted)
                            .Select(sc => new SubCategoryResponseDto
                            {
                                Id = sc.Id,
                                Name = sc.Name,
                                CategoryId = sc.CategoryId,
                                CategoryName = sc.Category.Name
                            })
                            .ToList()
                    })
                    .ToListAsync();

                return new PagedResponse<ProductResponseDto>
                {
                    Items = products,

                    Page = hasFilters ? 1 : page,

                    PageSize = hasFilters
                        ? products.Count
                        : pageSize,

                    TotalCount = totalCount,

                    TotalPages = totalPages,

                    HasMore =
                        !hasFilters &&
                        page < totalPages
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

             

                throw;
            }
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
        [HttpGet("by-name")]
        public async Task<ActionResult<ProductResponseDto>> GetProductByName(
    [FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Product name is required.");
            }

            var product =
                await _productService.GetProductsByName(name);

            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
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