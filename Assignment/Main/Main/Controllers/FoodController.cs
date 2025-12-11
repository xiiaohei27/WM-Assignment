using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Main.Controllers;

public class FoodController(DB db) : Controller
{
    // GET: Food/Index - Display all food items by category
    public IActionResult Index(string? categoryId)
    {
        var categories = db.FoodCategories
            .Include(c => c.FoodItems)
            .ToList();

        var foodItems = string.IsNullOrEmpty(categoryId)
            ? db.FoodItems.Include(f => f.Category).Where(f => f.IsAvailable).ToList()
            : db.FoodItems.Include(f => f.Category).Where(f => f.CategoryId == categoryId && f.IsAvailable).ToList();

        ViewBag.Categories = categories;
        ViewBag.SelectedCategoryId = categoryId;

        return View(foodItems);
    }

    // POST: Food/AddToCart
    [HttpPost]
    public IActionResult AddToCart(string foodItemId, int quantity = 1)
    {
        var foodItem = db.FoodItems.Find(foodItemId);
        if (foodItem == null || !foodItem.IsAvailable)
        {
            TempData["Error"] = "Food item not found or unavailable.";
            return RedirectToAction("Index");
        }

        // Get or create cart from session
        var cart = HttpContext.Session.GetObject<List<CartItem>>("FoodCart") ?? new List<CartItem>();

        // Check if item already in cart
        var existingItem = cart.FirstOrDefault(c => c.FoodItemId == foodItemId);
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            cart.Add(new CartItem
            {
                FoodItemId = foodItemId,
                Name = foodItem.Name,
                Price = foodItem.Price,
                Quantity = quantity,
                Image = foodItem.Image
            });
        }

        // Save cart back to session
        HttpContext.Session.SetObject("FoodCart", cart);

        TempData["Info"] = $"{foodItem.Name} added to cart!";
        return RedirectToAction("Index");
    }

    // GET: Food/Cart
    public IActionResult Cart()
    {
        var cart = HttpContext.Session.GetObject<List<CartItem>>("FoodCart") ?? new List<CartItem>();
        return View(cart);
    }

    // POST: Food/UpdateCart
    [HttpPost]
    public IActionResult UpdateCart(string foodItemId, int quantity)
    {
        var cart = HttpContext.Session.GetObject<List<CartItem>>("FoodCart") ?? new List<CartItem>();
        var item = cart.FirstOrDefault(c => c.FoodItemId == foodItemId);

        if (item != null)
        {
            if (quantity <= 0)
            {
                cart.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
            }
            HttpContext.Session.SetObject("FoodCart", cart);
        }

        return RedirectToAction("Cart");
    }

    // POST: Food/RemoveFromCart
    [HttpPost]
    public IActionResult RemoveFromCart(string foodItemId)
    {
        var cart = HttpContext.Session.GetObject<List<CartItem>>("FoodCart") ?? new List<CartItem>();
        var item = cart.FirstOrDefault(c => c.FoodItemId == foodItemId);

        if (item != null)
        {
            cart.Remove(item);
            HttpContext.Session.SetObject("FoodCart", cart);
            TempData["Info"] = "Item removed from cart.";
        }

        return RedirectToAction("Cart");
    }

    // GET: Food/Checkout
    public IActionResult Checkout()
    {
        var cart = HttpContext.Session.GetObject<List<CartItem>>("FoodCart") ?? new List<CartItem>();

        if (!cart.Any())
        {
            TempData["Error"] = "Your cart is empty!";
            return RedirectToAction("Index");
        }

        return View(cart);
    }

    // POST: Food/PlaceOrder
    [HttpPost]
    public IActionResult PlaceOrder(string paymentMethod)
    {
        var cart = HttpContext.Session.GetObject<List<CartItem>>("FoodCart") ?? new List<CartItem>();

        if (!cart.Any())
        {
            TempData["Error"] = "Your cart is empty!";
            return RedirectToAction("Index");
        }

        // Create food order
        var order = new FoodOrder
        {
            Id = Guid.NewGuid().ToString(),
            UserId = User.Identity?.IsAuthenticated == true ? User.Identity.Name : null,
            OrderDateTime = DateTime.Now,
            TotalAmount = cart.Sum(c => c.Price * c.Quantity),
            Status = "Pending",
            PaymentMethod = paymentMethod
        };

        db.FoodOrders.Add(order);

        // Create order items
        foreach (var cartItem in cart)
        {
            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid().ToString(),
                FoodOrderId = order.Id,
                FoodItemId = cartItem.FoodItemId,
                Quantity = cartItem.Quantity,
                UnitPrice = cartItem.Price,
                Subtotal = cartItem.Price * cartItem.Quantity
            };
            db.OrderItems.Add(orderItem);
        }

        db.SaveChanges();

        // Clear cart
        HttpContext.Session.Remove("FoodCart");

        TempData["Info"] = "Order placed successfully!";
        return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
    }

    // GET: Food/OrderConfirmation
    public IActionResult OrderConfirmation(string orderId)
    {
        var order = db.FoodOrders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.FoodItem)
            .FirstOrDefault(o => o.Id == orderId);

        if (order == null)
        {
            return RedirectToAction("Index");
        }

        return View(order);
    }

    // POST: Food/ClearCart
    [HttpPost]
    public IActionResult ClearCart()
    {
        HttpContext.Session.Remove("FoodCart");
        TempData["Info"] = "Cart cleared.";
        return RedirectToAction("Index");
    }
}