using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Data;
using PharmacyAPI.DTOs;
using PharmacyAPI.Models;
using PharmacyAPI.Services;
using System.IO;

namespace PharmacyAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {

        private readonly PharmacyDbContext _context;
        private readonly IProductService productService;

        public ProductsController( 
            IProductService product, PharmacyDbContext context
            )
        {
            productService = product;
            _context = context;
           
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
        {
            var products = await productService.GetProducts();

            return products;
        }

        [HttpGet("checkProductExists/{productName}")]
        [Authorize]
        public async Task<ActionResult<bool>> CheckProductExists(string productName)
        {
            var check = await productService.CheckProductExists( productName);

            return check;
        }
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            var p = await productService.GetProduct(id);
            return p;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ProductDto>> CreateProduct([FromForm] ProductDto dto)
        {
            var product = await productService.CreateProduct(dto);
            return Ok(product);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] UpdateProductDto dto)
        {

            
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            

          await productService.UpdateProduct(id,dto,product);
            return Ok(product);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
         await productService.DeleteProduct(id,product);
            return NoContent();
        }

      
    }
}
