using Parcial2.Models;

namespace Parcial2.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (!db.Users.Any())
        {
            db.Users.AddRange(
                new User { Username = "admin",   PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),   Role = "Admin" },
                new User { Username = "cajero1", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Cajero123!"), Role = "Cashier" }
            );
            await db.SaveChangesAsync();
        }

        if (!db.Products.Any())
        {
            db.Products.AddRange(
                new Product { Name = "Leche Entera 1L",         Description = "Leche entera pasteurizada",      Category = "Lácteos",     Price = 2500, Stock = 50,  LowStockThreshold = 10 },
                new Product { Name = "Pan Integral 500g",       Description = "Pan integral de trigo",          Category = "Panadería",   Price = 3200, Stock = 30,  LowStockThreshold = 5  },
                new Product { Name = "Arroz Blanco 1kg",        Description = "Arroz blanco de grano largo",    Category = "Granos",      Price = 4800, Stock = 100, LowStockThreshold = 20 },
                new Product { Name = "Aceite de Girasol 900ml", Description = "Aceite vegetal de girasol",      Category = "Aceites",     Price = 7500, Stock = 40,  LowStockThreshold = 8  },
                new Product { Name = "Azúcar Blanca 1kg",       Description = "Azúcar refinada blanca",         Category = "Endulzantes", Price = 3900, Stock = 80,  LowStockThreshold = 15 },
                new Product { Name = "Café Molido 250g",        Description = "Café molido de origen",          Category = "Bebidas",     Price = 6200, Stock = 25,  LowStockThreshold = 5  },
                new Product { Name = "Jabón de Barra x3",       Description = "Jabón de baño, pack de 3",       Category = "Limpieza",    Price = 4100, Stock = 60,  LowStockThreshold = 10 },
                new Product { Name = "Papel Higiénico x4",      Description = "Papel higiénico, pack de 4",     Category = "Higiene",     Price = 5500, Stock = 70,  LowStockThreshold = 10 },
                new Product { Name = "Detergente 500g",         Description = "Detergente en polvo multiuso",   Category = "Limpieza",    Price = 4800, Stock = 3,   LowStockThreshold = 5  },
                new Product { Name = "Atún Enlatado 170g",      Description = "Atún en agua, lata 170g",        Category = "Enlatados",   Price = 3100, Stock = 45,  LowStockThreshold = 10 }
            );
            await db.SaveChangesAsync();
        }
    }
}
