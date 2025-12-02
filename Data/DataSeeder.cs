using Microsoft.AspNetCore.Identity;
using CongCuGoiYSanPham.Models;

namespace CongCuGoiYSanPham.Data
{
    public class DataSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Seed Roles
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }
            if (!await roleManager.RoleExistsAsync("Customer"))
            {
                await roleManager.CreateAsync(new IdentityRole("Customer"));
            }

            // Seed Admin User
            if (await userManager.FindByEmailAsync("admin@shop.com") == null)
            {
                var admin = new User
                {
                    UserName = "admin@shop.com",
                    Email = "admin@shop.com",
                    FullName = "Administrator",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(admin, "Admin@123");
                await userManager.AddToRoleAsync(admin, "Admin");
            }

            // Seed Customer User
            if (await userManager.FindByEmailAsync("customer@shop.com") == null)
            {
                var customer = new User
                {
                    UserName = "customer@shop.com",
                    Email = "customer@shop.com",
                    FullName = "John Doe",
                    Address = "123 Main St, Hanoi",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(customer, "Customer@123");
                await userManager.AddToRoleAsync(customer, "Customer");
            }

            // Seed Categories
            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Electronics", Description = "Electronic devices and accessories" },
                    new Category { Name = "Fashion", Description = "Clothing and accessories" },
                    new Category { Name = "Home & Living", Description = "Home decor and furniture" },
                    new Category { Name = "Books", Description = "Books and magazines" },
                    new Category { Name = "Sports", Description = "Sports equipment and accessories" }
                };
                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
            }

            // Seed Products
            if (!context.Products.Any())
            {
                var electronics = context.Categories.First(c => c.Name == "Electronics");
                var fashion = context.Categories.First(c => c.Name == "Fashion");
                var home = context.Categories.First(c => c.Name == "Home & Living");

                var products = new List<Product>
                {
                    new Product { Name = "Wireless Headphones", Description = "High-quality wireless headphones with noise cancellation", CategoryId = electronics.Id, ImageUrl = "https://res.cloudinary.com/dpmbko91g/image/upload/v1767427843/Wireless_Headphones_nfjlfh.jpg" },
                    new Product { Name = "Smart Watch", Description = "Fitness tracking smartwatch with heart rate monitor", CategoryId = electronics.Id, ImageUrl = "https://res.cloudinary.com/dpmbko91g/image/upload/v1767427930/Smart_Watch_tbgq3t.jpg" },
                    new Product { Name = "Laptop Stand", Description = "Ergonomic aluminum laptop stand", CategoryId = electronics.Id, ImageUrl = "https://res.cloudinary.com/dpmbko91g/image/upload/v1767427989/Laptop_Stand_ysmygz.jpg" },
                    new Product { Name = "T-Shirt", Description = "Cotton t-shirt available in multiple colors", CategoryId = fashion.Id, ImageUrl = "https://res.cloudinary.com/dpmbko91g/image/upload/v1767428054/T-Shirt_ok4f5y.jpg" },
                    new Product { Name = "Jeans", Description = "Classic denim jeans", CategoryId = fashion.Id, ImageUrl = "https://res.cloudinary.com/dpmbko91g/image/upload/v1767428127/Jeans_jp0c4t.jpg" },
                    new Product { Name = "Sneakers", Description = "Comfortable running sneakers", CategoryId = fashion.Id, ImageUrl = "https://res.cloudinary.com/dpmbko91g/image/upload/v1767428181/Sneakers_i8nskb.jpg" },
                    new Product { Name = "Coffee Maker", Description = "Automatic coffee maker with timer", CategoryId = home.Id, ImageUrl = "https://res.cloudinary.com/dpmbko91g/image/upload/v1767430254/Coffee_Maker_zvvzfg.jpg" },
                    new Product { Name = "Desk Lamp", Description = "LED desk lamp with adjustable brightness", CategoryId = home.Id, ImageUrl = "https://res.cloudinary.com/dpmbko91g/image/upload/v1767430365/Desk_Lamp_t4skuv.jpg" }
                };
                context.Products.AddRange(products);
                await context.SaveChangesAsync();

                // Seed Variants and Inventory
                foreach (var product in products)
                {
                    var variants = new List<Variant>();
                    
                    if (product.CategoryId == fashion.Id)
                    {
                        var sizes = new[] { "S", "M", "L", "XL" };
                        var colors = new[] { "Black", "White", "Blue" };
                        
                        foreach (var size in sizes)
                        {
                            foreach (var color in colors)
                            {
                                var variant = new Variant
                                {
                                    ProductId = product.Id,
                                    SKU = $"{product.Name.Replace(" ", "").ToUpper()}-{size}-{color.ToUpper()}",
                                    Size = size,
                                    Color = color,
                                    Price = new Random().Next(200000, 1000000),
                                    IsActive = true
                                };
                                variants.Add(variant);
                            }
                        }
                    }
                    else
                    {
                        var variant = new Variant
                        {
                            ProductId = product.Id,
                            SKU = $"{product.Name.Replace(" ", "").ToUpper()}-STD",
                            Size = "Standard",
                            Color = "Default",
                            Price = new Random().Next(500000, 5000000),
                            IsActive = true
                        };
                        variants.Add(variant);
                    }

                    context.Variants.AddRange(variants);
                    await context.SaveChangesAsync();

                    // Add inventory for each variant
                    foreach (var variant in variants)
                    {
                        context.Inventories.Add(new Inventory
                        {
                            VariantId = variant.Id,
                            Quantity = new Random().Next(50, 200),
                            ReorderLevel = 10
                        });
                    }
                }
                await context.SaveChangesAsync();
            }

            // Seed Promotions
            if (!context.Promotions.Any())
            {
                var promotions = new List<Promotion>
                {
                    new Promotion
                    {
                        Code = "WELCOME10",
                        Name = "Welcome Discount",
                        Description = "10% off for new customers",
                        DiscountType = "Percentage",
                        DiscountValue = 10,
                        MinOrderAmount = 100000,
                        MaxDiscountAmount = 50000,
                        StartDate = DateTime.UtcNow.AddDays(-30),
                        EndDate = DateTime.UtcNow.AddDays(30),
                        IsActive = true
                    },
                    new Promotion
                    {
                        Code = "FREESHIP",
                        Name = "Free Shipping",
                        Description = "Free shipping on orders over 500k",
                        DiscountType = "FixedAmount",
                        DiscountValue = 30000,
                        MinOrderAmount = 500000,
                        StartDate = DateTime.UtcNow.AddDays(-15),
                        EndDate = DateTime.UtcNow.AddDays(15),
                        IsActive = true
                    }
                };
                context.Promotions.AddRange(promotions);
                await context.SaveChangesAsync();
            }
        }
    }
}
