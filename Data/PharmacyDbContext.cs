using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Models;
using PharmacyAPI.Models.Authentication;

namespace PharmacyAPI.Data
{
    public class PharmacyDbContext  : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public PharmacyDbContext(DbContextOptions<PharmacyDbContext> options) : base(options)
        {
        }

       
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<Cart> Carts { get; set; } = null!;
        public DbSet<CartItem> CartItems { get; set; } = null!;
        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<SubCategory> SubCategories { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);





            // Category -> SubCategory
            modelBuilder.Entity<Category>()
                .HasMany(c => c.SubCategories)
                .WithOne(sc => sc.Category)
                .HasForeignKey(sc => sc.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);


            // Product <-> SubCategory
            modelBuilder.Entity<Product>()
                .HasMany(p => p.SubCategories)
                .WithMany(sc => sc.Products)
                .UsingEntity(j => j.ToTable("ProductSubCategories"));


            // Client -> Orders
            modelBuilder.Entity<Client>()
       .HasIndex(x => x.PhoneNumber)
       .IsUnique();

            modelBuilder.Entity<Client>()
                .HasMany(x => x.Orders)
                .WithOne(x => x.Client)
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);


            // Order -> OrderItems
            modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);


            // Product -> OrderItems
            modelBuilder.Entity<Product>()
                .HasMany(p => p.OrderItems)
                .WithOne(i => i.Product)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            //condition for subcategory 
            modelBuilder.Entity<SubCategory>()
    .HasQueryFilter(sc => !sc.IsDeleted);

            // Decimal precision
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(i => i.UnitPrice)
                .HasPrecision(18, 2);
            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Pain Relief" },
                new Category { Id = 2, Name = "Vitamins" },
                new Category { Id = 3, Name = "First Aid" }
            );

        }
    }
}
