using Main.Models;

namespace Main;

public static class FoodDataSeeder
{
    public static void SeedFoodData(DB db)
    {
        // Check if data already exists
        if (db.FoodCategories.Any())
        {
            return; // Data already seeded
        }

        // Create Food Categories
        var popcorn = new FoodCategory
        {
            Id = "1",
            Name = "Popcorn",
            Description = "Fresh popped popcorn in various flavors"
        };

        var drinks = new FoodCategory
        {
            Id = "2",
            Name = "Drinks",
            Description = "Refreshing beverages"
        };

        var snacks = new FoodCategory
        {
            Id = "3",
            Name = "Snacks",
            Description = "Delicious movie snacks"
        };

        var combo = new FoodCategory
        {
            Id = "4",
            Name = "Combo",
            Description = "Value combo deals"
        };

        db.FoodCategories.AddRange(popcorn, drinks, snacks, combo);

        // Create Food Items
        var foodItems = new List<FoodItem>
        {
            // Popcorn
            new FoodItem
            {
                Id = "1",
                Name = "Classic Popcorn (Small)",
                Description = "Lightly salted classic popcorn",
                Price = 8.00M,
                CategoryId = popcorn.Id,
                Image = "popcorn-small.jpg",
                IsAvailable = true
            },
            new FoodItem
            {
                Id = "2",
                Name = "Classic Popcorn (Large)",
                Description = "Lightly salted classic popcorn",
                Price = 12.00M,
                CategoryId = popcorn.Id,
                Image = "popcorn-large.jpg",
                IsAvailable = true
            },
            new FoodItem
            {
                Id = "3",
                Name = "Caramel Popcorn",
                Description = "Sweet caramel coated popcorn",
                Price = 15.00M,
                CategoryId = popcorn.Id,
                Image = "caramel-popcorn.jpg",
                IsAvailable = true
            },

            // Drinks
            new FoodItem
            {
                Id = "4",
                Name = "Soft Drink (Small)",
                Description = "Coca-Cola, Pepsi, Sprite, or Fanta",
                Price = 6.00M,
                CategoryId = drinks.Id,
                Image = "soda-small.jpg",
                IsAvailable = true
            },
            new FoodItem
            {
                Id = "5",
                Name = "Soft Drink (Large)",
                Description = "Coca-Cola, Pepsi, Sprite, or Fanta",
                Price = 9.00M,
                CategoryId = drinks.Id,
                Image = "soda-large.jpg",
                IsAvailable = true
            },
            new FoodItem
            {
                Id = "6",
                Name = "Mineral Water",
                Description = "500ml bottled water",
                Price = 4.00M,
                CategoryId = drinks.Id,
                Image = "water.jpg",
                IsAvailable = true
            },

            // Snacks
            new FoodItem
            {
                Id = "7",
                Name = "Nachos with Cheese",
                Description = "Crispy nachos with warm cheese dip",
                Price = 12.00M,
                CategoryId = snacks.Id,
                Image = "nachos.jpg",
                IsAvailable = true
            },
            new FoodItem
            {
                Id = "8",
                Name = "Hot Dog",
                Description = "Grilled hot dog with condiments",
                Price = 10.00M,
                CategoryId = snacks.Id,
                Image = "hotdog.jpg",
                IsAvailable = true
            },
            new FoodItem
            {
                Id = "9",
                Name = "Pretzel",
                Description = "Warm salted pretzel",
                Price = 8.00M,
                CategoryId = snacks.Id,
                Image = "pretzel.jpg",
                IsAvailable = true
            },

            // Combos
            new FoodItem
            {
                Id = "10",
                Name = "Movie Combo",
                Description = "Large popcorn + Large drink",
                Price = 18.00M,
                CategoryId = combo.Id,
                Image = "combo-movie.jpg",
                IsAvailable = true
            },
            new FoodItem
            {
                Id = "11",
                Name = "Family Combo",
                Description = "2 Large popcorn + 2 Large drinks + Nachos",
                Price = 45.00M,
                CategoryId = combo.Id,
                Image = "combo-family.jpg",
                IsAvailable = true
            }
        };

        db.FoodItems.AddRange(foodItems);
        db.SaveChanges();
    }
}