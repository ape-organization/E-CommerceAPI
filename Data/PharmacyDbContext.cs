using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Models;
using PharmacyAPI.Models.Authentication;

namespace PharmacyAPI.Data
{
    public class PharmacyDbContext
        : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public PharmacyDbContext(
            DbContextOptions<PharmacyDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<Cart> Carts { get; set; } = null!;
        public DbSet<Slider> Sliders { get; set; } = null!;
        public DbSet<CartItem> CartItems { get; set; } = null!;
        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<SubCategory> SubCategories { get; set; } = null!;
        public DbSet<Brand> Brand { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================================
            // Identity Roles
            // ============================================

            modelBuilder.Entity<ApplicationRole>().HasData(
                new ApplicationRole
                {
                    Id = "role-user",
                    Name = "User",
                    NormalizedName = "USER",
                    ConcurrencyStamp =
                        "11111111-1111-1111-1111-111111111111"
                },
                new ApplicationRole
                {
                    Id = "role-admin",
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    ConcurrencyStamp =
                        "22222222-2222-2222-2222-222222222222"
                }
            );


            // ============================================
            // Category -> SubCategory
            // ============================================

            modelBuilder.Entity<Category>()
                .HasMany(c => c.SubCategories)
                .WithOne(sc => sc.Category)
                .HasForeignKey(sc => sc.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);


            // ============================================
            // Product <-> SubCategory
            // ============================================

            modelBuilder.Entity<Product>()
                .HasMany(p => p.SubCategories)
                .WithMany(sc => sc.Products)
                .UsingEntity(j => j.ToTable("ProductSubCategories"));

            modelBuilder.Entity<Product>()
    .HasOne(p => p.Brand)
    .WithMany(b => b.Products)
    .HasForeignKey(p => p.BrandId)
    .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
    .Property(p => p.DiscountPercentage)
    .HasPrecision(5, 2);


            // ============================================
            // Client
            // ============================================

            modelBuilder.Entity<Client>()
                .HasIndex(x => x.PhoneNumber)
                .IsUnique();

            modelBuilder.Entity<Client>()
                .HasMany(x => x.Orders)
                .WithOne(x => x.Client)
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);


            // ============================================
            // Order -> OrderItems
            // ============================================

            modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);


            // ============================================
            // Product -> OrderItems
            // ============================================

            modelBuilder.Entity<Product>()
                .HasMany(p => p.OrderItems)
                .WithOne(i => i.Product)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);


            // ============================================
            // SubCategory Query Filter
            // ============================================

            modelBuilder.Entity<SubCategory>()
                .HasQueryFilter(sc => !sc.IsDeleted);


            // ============================================
            // Decimal Precision
            // ============================================

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(i => i.UnitPrice)
                .HasPrecision(18, 2);


            // ============================================
            // Seed Categories
            // ============================================

            modelBuilder.Entity<Category>().HasData(
                new Category
                {
                    Id = 1,
                    NameEn = "Pain Relief",
                    NameAr = "الالام ",
                },
                new Category
                {
                    Id = 2,
                    NameEn = "Vitamins",
                    NameAr = "فيتامينات",
                },
                new Category
                {
                    Id = 3,
                    NameEn = "First Aid",
                    NameAr = "اسعافات اوليه",
                }
            );
           
        }
    }
}