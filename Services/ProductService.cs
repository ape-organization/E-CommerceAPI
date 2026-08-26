using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Data;
using PharmacyAPI.Models;
using PharmacyAPI.Models.RequestsModels;

namespace PharmacyAPI.Services
{
    public interface IProductService
    {
        Task<List<ProductResponseDto>> GetProducts(
     int? categoryId = null,
     int? subCategoryId = null,
     int? brandId = null,
     bool? offers = null);

        Task<ProductResponseDto?> GetProduct(int id);

        Task<ProductDto> CreateProduct(ProductDto dto);

        Task<bool> UpdateProduct(
            int id,
            UpdateProductDto dto);

        Task<bool> DeleteProduct(int id);

        Task<bool> CheckProductExists(string name);
        Task<IEnumerable<Product>> GetDiscountedProducts();

        Task<bool> RemoveDiscount(int id);
        Task<List<ProductResponseDto>> GetProductsByIds(
    List<int> productIds
);
    }


    public class ProductService : IProductService
    {
        private readonly PharmacyDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProductService(
            PharmacyDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }
        // ============================================
        // GET PRODUCTS BY IDS
        // ============================================

        public async Task<List<ProductResponseDto>> GetProductsByIds(
            List<int> productIds)
        {
            if (productIds == null || productIds.Count == 0)
            {
                return new List<ProductResponseDto>();
            }

            // Remove duplicates and invalid IDs
            var ids = productIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
            {
                return new List<ProductResponseDto>();
            }

            return await _context.Products
                .AsNoTracking()
                .Where(p =>
                    ids.Contains(p.Id) &&
                    !p.IsDeleted)
                .Select(p => new ProductResponseDto
                {
                    // ========================================
                    // BASIC INFORMATION
                    // ========================================

                    Id = p.Id,

                    Name = p.Name,

                    Description = p.Description,

                    Price = p.Price,

                    DiscountPercentage =
                        p.DiscountPercentage,

                    StockQuantity =
                        p.StockQuantity,

                    IsInStock =
                        p.IsInStock,

                    ImageUrl =
                        p.ImageUrl,


                    // ========================================
                    // BRAND
                    // ========================================

                    BrandId =
                        p.BrandId,

                    Brand = p.Brand == null
                        ? null
                        : new BrandResponseDto
                        {
                            Id = p.Brand.Id,

                            Name = p.Brand.Name,

                            ImageUrl =
                                p.Brand.ImageUrl
                        },


                    // ========================================
                    // CATEGORY
                    // ========================================

                    CategoryId =
                        p.SubCategories
                            .Where(sc => !sc.IsDeleted)
                            .Select(sc => (int?)sc.CategoryId)
                            .FirstOrDefault(),


                    // ========================================
                    // SUBCATEGORIES
                    // ========================================

                    SubCategories =
                        p.SubCategories
                            .Where(sc => !sc.IsDeleted)
                            .Select(sc => new SubCategoryResponseDto
                            {
                                Id = sc.Id,

                                Name = sc.Name,

                                CategoryId =
                                    sc.CategoryId,

                                CategoryName =
                                    sc.Category.Name

                            })
                            .ToList()
                })
                .ToListAsync();
        }
        public async Task<bool> RemoveDiscount(int id)
        {
            var product =
                await _context.Products
                    .FirstOrDefaultAsync(p =>
                        p.Id == id &&
                        !p.IsDeleted);

            if (product == null)
            {
                return false;
            }

            product.DiscountPercentage = 0;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<Product>> GetDiscountedProducts()
        {
            return await _context.Products
                .Where(p =>
                    !p.IsDeleted &&
                    p.DiscountPercentage > 0)
                .Include(p => p.Brand)
                .Include(p => p.SubCategories)
                .ToListAsync();
        }

        // ============================================
        // GET ALL PRODUCTS
        // ============================================

        public async Task<List<ProductResponseDto>> GetProducts(
      int? categoryId = null,
      int? subCategoryId = null,
      int? brandId = null,
      bool? offers = null)
        {
            var query = _context.Products
                .AsNoTracking()
                .Where(p => !p.IsDeleted);


            // =========================
            // CATEGORY FILTER
            // =========================

            if (categoryId.HasValue)
            {
                query = query.Where(p =>
                    p.SubCategories.Any(sc =>
                        !sc.IsDeleted &&
                        sc.CategoryId == categoryId.Value
                    )
                );
            }


            // =========================
            // SUBCATEGORY FILTER
            // =========================

            if (subCategoryId.HasValue)
            {
                query = query.Where(p =>
                    p.SubCategories.Any(sc =>
                        !sc.IsDeleted &&
                        sc.Id == subCategoryId.Value
                    )
                );
            }


            // =========================
            // BRAND FILTER
            // =========================

            if (brandId.HasValue)
            {
                query = query.Where(p =>
                    p.BrandId == brandId.Value
                );
            }


            // =========================
            // OFFERS FILTER
            // =========================

            if (offers == true)
            {
                query = query.Where(p =>
                    p.DiscountPercentage > 0
                );
            }


            // =========================
            // RESPONSE
            // =========================

            return await query

                .Select(p => new ProductResponseDto
                {
                    // =========================
                    // BASIC INFORMATION
                    // =========================

                    Id = p.Id,

                    Name = p.Name,

                    Description = p.Description,

                    Price = p.Price,

                    DiscountPercentage =
                        p.DiscountPercentage,

                    StockQuantity =
                        p.StockQuantity,

                    IsInStock =
                        p.IsInStock,

                    ImageUrl =
                        p.ImageUrl,


                    // =========================
                    // BRAND
                    // =========================

                    BrandId =
                        p.BrandId,

                    Brand = p.Brand == null
                        ? null
                        : new BrandResponseDto
                        {
                            Id = p.Brand.Id,

                            Name = p.Brand.Name,

                            ImageUrl =
                                p.Brand.ImageUrl
                        },


                    // =========================
                    // CATEGORY
                    // =========================

                    CategoryId = p.SubCategories
                        .Where(sc => !sc.IsDeleted)
                        .Select(sc => (int?)sc.CategoryId)
                        .FirstOrDefault(),


                    // =========================
                    // SUBCATEGORIES
                    // =========================

                    SubCategories = p.SubCategories
                        .Where(sc => !sc.IsDeleted)

                        .Select(sc => new SubCategoryResponseDto
                        {
                            Id = sc.Id,

                            Name = sc.Name,

                            CategoryId =
                                sc.CategoryId,

                            CategoryName =
                                sc.Category.Name
                        })

                        .ToList()
                })

                .ToListAsync();
        }

        // ============================================
        // GET PRODUCT BY ID
        // ============================================

        public async Task<ProductResponseDto?> GetProduct(int id)
        {
            return await _context.Products
                .AsNoTracking()
                .Where(p =>
                    p.Id == id &&
                    !p.IsDeleted)
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,

                    Name = p.Name,

                    Description = p.Description,

                    Price = p.Price,
                    IsInStock = p.IsInStock,
                    StockQuantity = p.StockQuantity,
                    

                    ImageUrl = p.ImageUrl,

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
                .FirstOrDefaultAsync();
        }
        // ============================================
        // CHECK PRODUCT EXISTS
        // ============================================

        public async Task<bool> CheckProductExists(string name)
        {
            return await _context.Products
                .AnyAsync(p =>
                    !p.IsDeleted &&
                    p.Name.ToLower() == name.ToLower());
        }


        // ============================================
        // CREATE PRODUCT
        // ============================================

        public async Task<ProductDto> CreateProduct(
            ProductDto dto)
        {
            if (dto.DiscountPercentage < 0 ||
    dto.DiscountPercentage > 100)
            {
                throw new InvalidOperationException(
                    "Discount percentage must be between 0 and 100."
                );
            }

            // Check product name
            var nameExists = await CheckProductExists(dto.Name);

            if (nameExists)
                throw new InvalidOperationException(
                    "A product with this name already exists.");


            // Validate subcategories
            var subCategories = await _context.SubCategories
                .Where(sc =>
                    dto.SubCategoryIds.Contains(sc.Id) &&
                    !sc.IsDeleted)
                .ToListAsync();


            if (subCategories.Count != dto.SubCategoryIds.Distinct().Count())
            {
                throw new KeyNotFoundException(
                    "One or more subcategories were not found.");
            }


            // Save image
            string? imageUrl = null;

            if (dto.Image != null)
            {
                imageUrl = await SaveImage(dto.Image);
            }


            // Create product
            var product = new Product
            {
                Name = dto.Name.Trim(),

                Description = dto.Description,
                IsInStock = dto.IsInStock,
                Price = dto.Price,

                StockQuantity = dto.StockQuantity,
                BrandId=dto.BrandId,
                ImageUrl = imageUrl,
                DiscountPercentage = dto.DiscountPercentage
            };


            // Many-to-many relationship
            foreach (var subCategory in subCategories)
            {
                product.SubCategories.Add(subCategory);
            }


            _context.Products.Add(product);

            await _context.SaveChangesAsync();


            // Return actual saved product
            return new ProductDto
            {
                Id = product.Id,

                Name = product.Name,

                Description = product.Description,

                Price = product.Price,

                StockQuantity = product.StockQuantity,

                ImageUrl = product.ImageUrl,
                IsInStock = product.IsInStock,

                SubCategoryIds = product.SubCategories
                    .Select(sc => sc.Id)
                    .ToList()
            };
        }


        // ============================================
        // UPDATE PRODUCT
        // ============================================

        public async Task<bool> UpdateProduct(
            int id,
            UpdateProductDto dto)
        {

            if (dto.DiscountPercentage < 0 ||
    dto.DiscountPercentage > 100)
            {
                throw new InvalidOperationException(
                    "Discount percentage must be between 0 and 100."
                );
            }


            var product = await _context.Products
                .Include(p => p.SubCategories)
                .FirstOrDefaultAsync(p =>
                    p.Id == id &&
                    !p.IsDeleted);


            if (product == null)
                return false;


            // Check duplicate name
            var nameExists = await _context.Products
                .AnyAsync(p =>
                    p.Id != id &&
                    !p.IsDeleted &&
                    p.Name.ToLower() == dto.Name.ToLower());


            if (nameExists)
            {
                throw new InvalidOperationException(
                    "A product with this name already exists.");
            }


            // Validate subcategories
            var subCategoryIds =
                dto.SubCategoryIds
                    .Distinct()
                    .ToList();


            var subCategories = await _context.SubCategories
                .Where(sc =>
                    subCategoryIds.Contains(sc.Id) &&
                    !sc.IsDeleted)
                .ToListAsync();


            if (subCategories.Count != subCategoryIds.Count)
            {
                throw new KeyNotFoundException(
                    "One or more subcategories were not found.");
            }


            // Update image
            if (dto.Image != null)
            {
                product.ImageUrl =
                    await SaveImage(dto.Image);
            }


            // Update basic properties
            product.Name = dto.Name.Trim();

            product.Description = dto.Description;

            product.Price = dto.Price;

            product.StockQuantity = dto.StockQuantity;
            product.DiscountPercentage =
    dto.DiscountPercentage;
            product.BrandId = dto.BrandId;
            product.IsInStock = dto.IsInStock;

            // ============================================
            // UPDATE MANY-TO-MANY RELATIONSHIP
            // ============================================

            product.SubCategories.Clear();

            foreach (var subCategory in subCategories)
            {
                product.SubCategories.Add(subCategory);
            }


            await _context.SaveChangesAsync();

            return true;
        }


        // ============================================
        // DELETE PRODUCT
        // ============================================

        public async Task<bool> DeleteProduct(int id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.Id == id &&
                    !p.IsDeleted);


            if (product == null)
                return false;


            // Soft delete only
            product.IsDeleted = true;


            await _context.SaveChangesAsync();

            return true;
        }


        // ============================================
        // SAVE IMAGE
        // ============================================

        private async Task<string> SaveImage(
            IFormFile image)
        {
            var folder =
                Path.Combine(
                    _environment.WebRootPath,
                    "images");


            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }


            var fileName =
                Guid.NewGuid().ToString()
                + Path.GetExtension(image.FileName);


            var filePath =
                Path.Combine(
                    folder,
                    fileName);


            await using var stream =
                new FileStream(
                    filePath,
                    FileMode.Create);


            await image.CopyToAsync(stream);


            return "/images/" + fileName;
        }
    }
}