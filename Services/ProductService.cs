using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Data;
using PharmacyAPI.Models;
using PharmacyAPI.Models.RequestsModels;
using PharmacyAPI.Models.Responses;
using System.Linq.Expressions;

namespace PharmacyAPI.Services
{
    public interface IProductService
    {
        Task<PagedResponse<ProductResponseDto>> GetProducts(
            int page = 1,
            int? categoryId = null,
            int? subCategoryId = null,
            int? brandId = null,
            bool? offers = null,
            CancellationToken cancellationToken = default);

        Task<ProductResponseDto?> GetProduct(
            int id,
            CancellationToken cancellationToken = default);

        Task<ProductDto> CreateProduct(
            ProductDto dto,
            CancellationToken cancellationToken = default);

        Task<bool> UpdateProduct(
            int id,
            UpdateProductDto dto,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteProduct(
            int id,
            CancellationToken cancellationToken = default);

        Task<bool> CheckProductExists(
            string name,
            CancellationToken cancellationToken = default);

        Task<List<ProductResponseDto>> GetDiscountedProducts(
            CancellationToken cancellationToken = default);

        Task<List<ProductResponseDto>> GetProductsByName(
            string name,
            CancellationToken cancellationToken = default);

        Task<ProductResponseDto?> RemoveDiscount(
            int id,
            CancellationToken cancellationToken = default);

        Task<List<ProductResponseDto>> GetProductsByIds(
            List<int> productIds,
            CancellationToken cancellationToken = default);
        Task<List<ProductResponseDto>> GetBestSellerProducts(
    int count = 10,
    CancellationToken cancellationToken = default);
    }

    public class ProductService : IProductService
    {
        private readonly PharmacyDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProductService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ImageService _imageService;
        public ProductService(
            ImageService imageService,
            IConfiguration configuration,
            PharmacyDbContext context,
            IWebHostEnvironment environment,
            ILogger<ProductService> logger)
        {
            _imageService = imageService;
            _configuration = configuration;
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        // =====================================================
        // GET PRODUCTS
        // =====================================================

        public async Task<PagedResponse<ProductResponseDto>> GetProducts(
            int page = 1,
            int? categoryId = null,
            int? subCategoryId = null,
            int? brandId = null,
            bool? offers = null,
            CancellationToken cancellationToken = default)
        {
            page = Math.Max(page, 1);

            const int pageSize = 100;

            IQueryable<Product> query = _context.Products
                .AsNoTracking()
                .Where(p => !p.IsDeleted);

            // =================================================
            // FILTERS
            // =================================================

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

            // =================================================
            // CHECK IF FILTERING IS ACTIVE
            // =================================================

            bool hasFilters =
                categoryId.HasValue ||
                subCategoryId.HasValue ||
                brandId.HasValue ||
                offers == true;

            // =================================================
            // FILTERED REQUEST
            //
            // Return ALL matching products.
            // Angular can then filter locally.
            // =================================================

            if (hasFilters)
            {
                var filteredProducts = await query
                    .OrderBy(p => p.Id)
                    .Select(MapProduct())
                    .ToListAsync(cancellationToken);

                return new PagedResponse<ProductResponseDto>
                {
                    Items = filteredProducts,
                    TotalCount = filteredProducts.Count,
                    Page = 1,
                    PageSize = filteredProducts.Count,
                    TotalPages = filteredProducts.Count > 0 ? 1 : 0,
                    HasMore = false
                };
            }

            // =================================================
            // NORMAL PAGINATION
            // =================================================

            var totalCount = await query.CountAsync(cancellationToken);

            var totalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(totalCount / (double)pageSize);

            var products = await query
                .OrderBy(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(MapProduct())
                .ToListAsync(cancellationToken);

            return new PagedResponse<ProductResponseDto>
            {
                Items = products,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasMore = page < totalPages
            };
        }



        // =====================================================
        // GET PRODUCT BY ID
        // =====================================================

        public async Task<ProductResponseDto?> GetProduct(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .AsNoTracking()
                .Where(p =>
                    p.Id == id &&
                    !p.IsDeleted)
                .Select(MapProduct())
                .FirstOrDefaultAsync(cancellationToken);
        }

        // =====================================================
        // GET PRODUCTS BY NAME
        // =====================================================

        public async Task<List<ProductResponseDto>> GetProductsByName(
      string name,
      CancellationToken cancellationToken = default)
        {
            name = name.Trim();

            if (string.IsNullOrWhiteSpace(name))
                return new List<ProductResponseDto>();

            return await _context.Products
                .AsNoTracking()
                .Where(p =>
                    !p.IsDeleted &&
                    (
                        EF.Functions.Like(p.NameEn, $"%{name}%") ||
                        EF.Functions.Like(p.NameAr, $"%{name}%")
                    ))
           
                .Select(MapProduct()).Take(50)
                .ToListAsync(cancellationToken);
        }


        // =====================================================
        // GET PRODUCTS BY IDS
        // =====================================================

        public async Task<List<ProductResponseDto>> GetProductsByIds(
            List<int> productIds,
            CancellationToken cancellationToken = default)
        {
            if (productIds == null || productIds.Count == 0)
                return new List<ProductResponseDto>();

            var ids = productIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
                return new List<ProductResponseDto>();

            return await _context.Products
                .AsNoTracking()
                .Where(p =>
                    ids.Contains(p.Id) &&
                    !p.IsDeleted)
                .Select(MapProduct())
                .ToListAsync(cancellationToken);
        }

        // =====================================================
        // GET DISCOUNTED PRODUCTS
        // =====================================================

        public async Task<List<ProductResponseDto>> GetDiscountedProducts(
            CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .AsNoTracking()
                .Where(p =>
                    !p.IsDeleted &&
                    p.DiscountPercentage > 0)
              
                .Select(MapProduct())
                .ToListAsync(cancellationToken);
        }

        // =====================================================
        // GET BEST SELLER PRODUCTS
        // =====================================================

        public async Task<List<ProductResponseDto>> GetBestSellerProducts(
            int count = 10,
            CancellationToken cancellationToken = default)
        {
            if (count <= 0)
                count = 10;

            return await _context.OrderItems
                .AsNoTracking()
                .Where(oi =>
                    oi.Product != null &&
                    !oi.Product.IsDeleted &&
    oi.Order.Status == OrderStatus.Confirmed)
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    SoldQuantity = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.SoldQuantity)
                .Take(count)
                .Join(
                    _context.Products
                        .AsNoTracking()
                        .Where(p => !p.IsDeleted),
                    bestSeller => bestSeller.ProductId,
                    product => product.Id,
                    (bestSeller, product) => product
                )
                .Select(MapProduct())
                .ToListAsync(cancellationToken);
        }

        // =====================================================
        // CHECK PRODUCT EXISTS
        // =====================================================
        public async Task<bool> CheckProductExists(
            string name,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            name = name.Trim();

            return await _context.Products
                .AsNoTracking()
                .AnyAsync(
                    p =>
                        !p.IsDeleted &&
                        p.NameEn == name,
                    cancellationToken);
        }

        // =====================================================
        // CREATE PRODUCT
        // =====================================================

        public async Task<ProductDto> CreateProduct(
            ProductDto dto,
            CancellationToken cancellationToken = default)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            // =================================================
            // VALIDATION
            // =================================================

            if (string.IsNullOrWhiteSpace(dto.NameEn))
                throw new InvalidOperationException(
                    "الاسم بالانجليزيه مطلوب");

            if (string.IsNullOrWhiteSpace(dto.NameAr))
                throw new InvalidOperationException(
                    "الاسم بالعربية مطلوب");

            if (dto.Price < 0)
                throw new InvalidOperationException(
                    "السعر لا يمكن ان يكون بالسالب");

            if (dto.StockQuantity < 0)
                throw new InvalidOperationException(
                    "الكميه لا يمكن ان تكون اقل من الصفر");

            if (dto.DiscountPercentage < 0 ||
                dto.DiscountPercentage > 100)
            {
                throw new InvalidOperationException(
                    "النسبه يجب ان تكون من 0 الي 100");
            }

            var nameEn = dto.NameEn.Trim();

            // =================================================
            // DUPLICATE NAME
            // =================================================

            var duplicateName = await _context.Products
                .AsNoTracking()
                .AnyAsync(
                    p =>
                        !p.IsDeleted &&
                        p.NameEn == nameEn,
                    cancellationToken);

            if (duplicateName)
            {
                throw new InvalidOperationException(
                    "منتج بنفس الاسم بالانجليزيه موجود من قبل ");
            }

            // =================================================
            // CHECK BRAND
            // =================================================

            var brandExists = await _context.Brand
                .AsNoTracking()
                .AnyAsync(
                    b =>
                        b.Id == dto.BrandId &&
                        !b.IsDeleted,
                    cancellationToken);

            if (!brandExists)
                throw new KeyNotFoundException("العلامه التجارية غير متوفره");

            // =================================================
            // CHECK SUBCATEGORIES
            // =================================================

            var subCategoryIds = dto.SubCategoryIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            var subCategories = await _context.SubCategories
                .Where(sc =>
                    subCategoryIds.Contains(sc.Id) &&
                    !sc.IsDeleted)
                .ToListAsync(cancellationToken);

            if (subCategories.Count != subCategoryIds.Count)
            {
                throw new KeyNotFoundException(
                    "واحده او اكتر من الفئات الفرعيه متوفره");
            }

            // =================================================
            // IMAGE
            // =================================================

            string? imageUrl = null;

            try
            {
                if (dto.Image != null)
                {
                    imageUrl = await _imageService.SaveImageAsync(
                        dto.Image,"products",
                        cancellationToken);
                }

                // =================================================
                // CREATE PRODUCT
                // =================================================

                var product = new Product
                {
                    NameEn = nameEn,
                    NameAr = dto.NameAr.Trim(),

                    DescriptionEn =
                        string.IsNullOrWhiteSpace(dto.DescriptionEn)
                            ? null
                            : dto.DescriptionEn.Trim(),

                    DescriptionAr =
                        string.IsNullOrWhiteSpace(dto.DescriptionAr)
                            ? null
                            : dto.DescriptionAr.Trim(),

                    Price = dto.Price,
                    StockQuantity = dto.StockQuantity,
                    IsInStock = dto.IsInStock,
                    DiscountPercentage = dto.DiscountPercentage,
                    BrandId = dto.BrandId,
                    ImageUrl = imageUrl,
                    IsDeleted = false
                };

                foreach (var subCategory in subCategories)
                {
                    product.SubCategories.Add(subCategory);
                }

                _context.Products.Add(product);

                await _context.SaveChangesAsync(cancellationToken);

                // =================================================
                // RETURN DTO
                // =================================================

                dto.Id = product.Id;
                dto.ImageUrl = product.ImageUrl;

                return dto;
            }
            catch
            {
                // Delete uploaded image if DB operation fails
                if (!string.IsNullOrWhiteSpace(imageUrl))
                {
                    DeleteImage(imageUrl);
                }

                throw;
            }
        }

        // =====================================================
        // UPDATE PRODUCT
        // =====================================================

        public async Task<bool> UpdateProduct(
            int id,
            UpdateProductDto dto,
            CancellationToken cancellationToken = default)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            // =================================================
            // VALIDATION
            // =================================================

            if (string.IsNullOrWhiteSpace(dto.NameEn))
                throw new InvalidOperationException(
                    "الاسم بالانجليزيه مطلوب");

            if (string.IsNullOrWhiteSpace(dto.NameAr))
                throw new InvalidOperationException(
                    "الاسم بالعربيه مطلوب");

            if (dto.Price < 0)
                throw new InvalidOperationException(
                    "السعر لا يمكن ان يكون بالسالب");

            if (dto.StockQuantity < 0)
                throw new InvalidOperationException(
                    "الكميه لا يمكن ان تكون اقل من الصفر");

            if (dto.DiscountPercentage < 0 ||
                dto.DiscountPercentage > 100)
            {
                throw new InvalidOperationException(
                    "النسبه يجب ان تكون من 0 الي 100");
            }

            var nameEn = dto.NameEn.Trim();

            // =================================================
            // GET PRODUCT
            // =================================================

            var product = await _context.Products
                .Include(p => p.SubCategories)
                .FirstOrDefaultAsync(
                    p =>
                        p.Id == id &&
                        !p.IsDeleted,
                    cancellationToken);

            if (product is null)
                return false;

            // =================================================
            // DUPLICATE NAME
            // =================================================

            var duplicateName = await _context.Products
                .AsNoTracking()
                .AnyAsync(
                    p =>
                        p.Id != id &&
                        !p.IsDeleted &&
                        p.NameEn == nameEn,
                    cancellationToken);

            if (duplicateName)
            {
                throw new InvalidOperationException(
                    "منتج اخر بنفس الاسم الانجليزي موجود بالفعل ");
            }

            // =================================================
            // CHECK BRAND
            // =================================================

            var brandExists = await _context.Brand
                .AsNoTracking()
                .AnyAsync(
                    b =>
                        b.Id == dto.BrandId &&
                        !b.IsDeleted,
                    cancellationToken);

            if (!brandExists)
                throw new KeyNotFoundException("العلامه الفرعيه غير متوفره");

            // =================================================
            // CHECK SUBCATEGORIES
            // =================================================

            var subCategoryIds = dto.SubCategoryIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            var subCategories = await _context.SubCategories
                .Where(sc =>
                    subCategoryIds.Contains(sc.Id) &&
                    !sc.IsDeleted)
                .ToListAsync(cancellationToken);

            if (subCategories.Count != subCategoryIds.Count)
            {
                throw new KeyNotFoundException(
                    "واحد او اكتر من الفئات الفرعيه موجود");
            }

            // =================================================
            // IMAGE
            // =================================================

            var oldImageUrl = product.ImageUrl;
            string? newImageUrl = null;

            try
            {
                if (dto.Image != null)
                {
                    newImageUrl = await _imageService.SaveImageAsync(
                        dto.Image,"products",
                        cancellationToken);

                    product.ImageUrl = newImageUrl;
                }

                // =================================================
                // UPDATE BASIC DATA
                // =================================================

                product.NameEn = nameEn;
                product.NameAr = dto.NameAr.Trim();

                product.DescriptionEn =
                    string.IsNullOrWhiteSpace(dto.DescriptionEn)
                        ? null
                        : dto.DescriptionEn.Trim();

                product.DescriptionAr =
                    string.IsNullOrWhiteSpace(dto.DescriptionAr)
                        ? null
                        : dto.DescriptionAr.Trim();

                product.Price = dto.Price;
                product.StockQuantity = dto.StockQuantity;
                product.IsInStock = dto.IsInStock;
                product.DiscountPercentage = dto.DiscountPercentage;
                product.BrandId = dto.BrandId;

                // =================================================
                // UPDATE SUBCATEGORIES
                // =================================================

                product.SubCategories.Clear();

                foreach (var subCategory in subCategories)
                {
                    product.SubCategories.Add(subCategory);
                }

                // =================================================
                // SAVE
                // =================================================

                await _context.SaveChangesAsync(cancellationToken);

                // Delete old image only after successful DB update
                if (!string.IsNullOrWhiteSpace(oldImageUrl) &&
                    !string.Equals(
                        oldImageUrl,
                        newImageUrl,
                        StringComparison.OrdinalIgnoreCase))
                {
                    DeleteImage(oldImageUrl);
                }

                return true;
            }
            catch
            {
                // Remove newly uploaded image if DB update failed
                if (!string.IsNullOrWhiteSpace(newImageUrl))
                {
                    DeleteImage(newImageUrl);
                }

                throw;
            }
        }

        // =====================================================
        // REMOVE DISCOUNT
        // =====================================================

        public async Task<ProductResponseDto?> RemoveDiscount(
            int id,
            CancellationToken cancellationToken = default)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(
                    p =>
                        p.Id == id &&
                        !p.IsDeleted,
                    cancellationToken);

            if (product is null)
                return null;

            product.DiscountPercentage = 0;

            await _context.SaveChangesAsync(cancellationToken);

            return await GetProduct(id, cancellationToken);
        }

        // =====================================================
        // DELETE PRODUCT
        // =====================================================

        public async Task<bool> DeleteProduct(
            int id,
            CancellationToken cancellationToken = default)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(
                    p =>
                        p.Id == id &&
                        !p.IsDeleted,
                    cancellationToken);

            if (product is null)
                return false;

            product.IsDeleted = true;

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }

        // =====================================================
        // PRODUCT DTO PROJECTION
        // =====================================================

        private static Expression<Func<Product, ProductResponseDto>> MapProduct()
        {
            return p => new ProductResponseDto
            {
                Id = p.Id,

                NameEn = p.NameEn,
                NameAr = p.NameAr,

                DescriptionEn = p.DescriptionEn,
                DescriptionAr = p.DescriptionAr,

                Price = p.Price,
                StockQuantity = p.StockQuantity,
                IsInStock = p.IsInStock,

                ImageUrl = p.ImageUrl,
                
                DiscountPercentage = p.DiscountPercentage,
              

                BrandId = p.BrandId,

                Brand = p.Brand == null
                    ? null
                    : new BrandResponseDto
                    {
                        Id = p.Brand.Id,
                        NameEn = p.Brand.NameEn,
                        NameAr = p.Brand.NameAr,
                        ImageUrl = p.Brand.ImageUrl
                    },

                SubCategories = p.SubCategories
                    .Where(sc => !sc.IsDeleted)
                    .Select(sc => new SubCategoryResponseDto
                    {
                        Id = sc.Id,
                        NameEn = sc.NameEn,
                        NameAr = sc.NameAr,
                        CategoryId = sc.CategoryId
                    })
                    .ToList()
            };
        }

        // =====================================================
        // SAVE IMAGE
        // =====================================================

    //    private async Task<string> SaveImage(
    //        IFormFile image,
    //        CancellationToken cancellationToken)
    //    {
    //        if (image.Length <= 0)
    //            throw new InvalidOperationException(
    //                "Invalid image.");

    //        const long maxFileSize = 5 * 1024 * 1024;

    //        if (image.Length > maxFileSize)
    //            throw new InvalidOperationException(
    //                "Image size cannot exceed 5 MB.");

    //        var allowedExtensions = new[]
    //        {
    //            ".jpg",
    //            ".jpeg",
    //            ".png",
    //            ".webp"
    //        };

    //        var extension =
    //            Path.GetExtension(image.FileName)
    //                .ToLowerInvariant();

    //        if (!allowedExtensions.Contains(extension))
    //        {
    //            throw new InvalidOperationException(
    //                "Only JPG, JPEG, PNG and WEBP images are allowed.");
    //        }

    //        //var imagesFolder = Path.Combine(
    //        //    _environment.WebRootPath,
    //        //    "images");
    //        var imagesFolder = Path.Combine(
    //_configuration["FileStorage:UploadPath"]!,
    //"products");

    //        Directory.CreateDirectory(imagesFolder);

    //        var fileName =
    //            $"{Guid.NewGuid():N}{extension}";

    //        var filePath = Path.Combine(
    //            imagesFolder,
    //            fileName);

    //        await using var stream = new FileStream(
    //            filePath,
    //            FileMode.CreateNew,
    //            FileAccess.Write,
    //            FileShare.None,
    //            bufferSize: 64 * 1024,
    //            useAsync: true);

    //        await image.CopyToAsync(
    //            stream,
    //            cancellationToken);

    //        //return $"/images/{fileName}";
    //        return $"/uploads/E_Commerce/products/{fileName}";
    //    }

        // =====================================================
        // DELETE IMAGE
        // =====================================================

        private void DeleteImage(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return;

            try
            {
                var fileName = Path.GetFileName(
                    imageUrl);

                if (string.IsNullOrWhiteSpace(fileName))
                    return;


                var imagesFolder = Path.Combine(
    _configuration["FileStorage:UploadPath"]!,
    "products");
                //var imagesFolder = Path.Combine(
                //    _environment.WebRootPath,
                //    "images");

                var filePath = Path.Combine(
                    imagesFolder,
                    fileName);





                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to delete product image: {ImageUrl}",
                    imageUrl);
            }
        }
    }
}