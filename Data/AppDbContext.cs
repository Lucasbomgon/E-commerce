using Ecommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.Name).HasMaxLength(120).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(180).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(255).IsRequired();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(category => category.Name).HasMaxLength(100).IsRequired();
            entity.Property(category => category.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(product => product.Name).HasMaxLength(150).IsRequired();
            entity.Property(product => product.Description).HasMaxLength(1000);
            entity.Property(product => product.Price).HasPrecision(10, 2);
            entity.HasOne(product => product.Category)
                .WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasIndex(cart => cart.UserId).IsUnique();
            entity.HasOne(cart => cart.User)
                .WithOne(user => user.Cart)
                .HasForeignKey<Cart>(cart => cart.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasIndex(item => new { item.CartId, item.ProductId }).IsUnique();
            entity.HasOne(item => item.Cart)
                .WithMany(cart => cart.Items)
                .HasForeignKey(item => item.CartId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Product)
                .WithMany(product => product.CartItems)
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(order => order.TotalAmount).HasPrecision(10, 2);
            entity.HasOne(order => order.User)
                .WithMany(user => user.Orders)
                .HasForeignKey(order => order.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(item => item.UnitPrice).HasPrecision(10, 2);
            entity.Property(item => item.TotalPrice).HasPrecision(10, 2);
            entity.HasOne(item => item.Order)
                .WithMany(order => order.Items)
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Product)
                .WithMany(product => product.OrderItems)
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Eletronicos", Description = "Produtos eletronicos e acessorios" },
            new Category { Id = 2, Name = "Livros", Description = "Livros fisicos e digitais" },
            new Category { Id = 3, Name = "Casa", Description = "Itens para casa e organizacao" }
        );

        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                Name = "Fone Bluetooth",
                Description = "Fone sem fio com estojo de carregamento",
                Price = 199.90m,
                StockQuantity = 25,
                CategoryId = 1
            },
            new Product
            {
                Id = 2,
                Name = "Clean Code",
                Description = "Livro sobre boas praticas de desenvolvimento",
                Price = 129.90m,
                StockQuantity = 15,
                CategoryId = 2
            },
            new Product
            {
                Id = 3,
                Name = "Organizador de Mesa",
                Description = "Organizador compacto para escritorio",
                Price = 59.90m,
                StockQuantity = 40,
                CategoryId = 3
            }
        );
    }
}
