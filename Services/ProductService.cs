using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Data;
using PharmacyAPI.DTOs;
using PharmacyAPI.Models;

namespace PharmacyAPI.Services
{
    public interface IProductService
    {
        Task<List<ProductDto>> GetProducts();
        Task<ProductDto> GetProduct(int id);
        Task<ProductDto> CreateProduct([FromForm] ProductDto dto);
        Task UpdateProduct(int id, [FromForm] UpdateProductDto dto, Product product);
        Task DeleteProduct(int id, Product product);
        Task<bool> CheckProductExists(string name);

    }
    public class ProductService:IProductService
    {
        private readonly PharmacyDbContext _context;
        private readonly IWebHostEnvironment _environment;
        public ProductService(PharmacyDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }
        public async Task<List<ProductDto>> GetProducts()
        {
            var products = await _context.Products
    .Where(p => !p.IsDeleted)
  .Select(p => new ProductDto
  {
      Id = p.Id,
      Name = p.Name,
      Description = p.Description,
      Price = p.Price,
      StockQuantity = p.StockQuantity,
      ImageUrl = p.ImageUrl,
      CategoryId = p.CategoryId,
      CategoryName = p.Category.Name
  })
    .ToListAsync();

            return products;
        }

        public async Task<ProductDto> GetProduct(int id)
        {
            var p = await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id
            && p.IsDeleted == false);
            if (p == null) return(null) ;

            return new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name
            };
        }

        public async Task<bool> CheckProductExists(string name)
        {
            var p = await _context.Products.FirstOrDefaultAsync(p => p.Name == name
            && p.IsDeleted == false);
            if (p == null) return false;

            return true;
        }

        public async Task<ProductDto> CreateProduct([FromForm] ProductDto dto)
        {
            string? imageUrl = null;
            if (dto.Image != null)
            {
                imageUrl = await SaveImage(dto.Image);
            }

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                CategoryId = dto.CategoryId,
                ImageUrl = imageUrl
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return dto;
        }

        public async Task UpdateProduct(int id, [FromForm] UpdateProductDto dto,Product product)
        {
        

            if (dto.Image != null)
            {
                product.ImageUrl = await SaveImage(dto.Image);
            }

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;
            product.CategoryId = dto.CategoryId;

            await _context.SaveChangesAsync();
          
        }

        public async Task DeleteProduct(int id,Product product)
        {
            
            product.IsDeleted = true;
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
         
        }

        private async Task<string> SaveImage(IFormFile image)
        {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
            var filePath = Path.Combine(_environment.WebRootPath, "images", fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }
            return "/images/" + fileName;
        }
    }
}
