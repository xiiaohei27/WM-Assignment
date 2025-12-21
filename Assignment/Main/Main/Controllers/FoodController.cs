using Main.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace Main.Controllers;

public class FoodController : Controller
{
    private readonly DB db;
    private readonly Helper hp;

    public FoodController(DB db, Helper hp)
    {
        this.db = db;
        this.hp = hp;
    }

    // GET: Food/Index
    public IActionResult Index(string? categoryId)
    {
        var items = db.FoodItems
            .Include(f => f.Category)
            .Where(f => f.IsAvailable)
            .AsQueryable();

        if (!string.IsNullOrEmpty(categoryId))
        {
            items = items.Where(f => f.CategoryId == categoryId);
        }

        ViewBag.Categories = db.FoodCategories.OrderBy(c => c.Name).ToList();
        ViewBag.SelectedCategoryId = categoryId;

        return View(items.OrderBy(f => f.Name).ToList());
    }


    // POST: Food/AddToCart
    [Authorize(Roles = "Member")]
    [HttpPost]
    public IActionResult AddToCart(string foodItemId, int quantity = 1)
    {
        var item = db.FoodItems.Find(foodItemId);
        if (item == null || !item.IsAvailable)
        {
            TempData["Error"] = "Item not available.";
            return RedirectToAction("Index");
        }

        var cart = HttpContext.Session.GetObject<List<CartItem>>("FoodCart") ?? new List<CartItem>();

        var existingItem = cart.FirstOrDefault(c => c.FoodItemId == foodItemId);
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            cart.Add(new CartItem
            {
                FoodItemId = item.Id,
                Name = item.Name,
                Price = item.Price,
                Quantity = quantity,
                Image = item.Image
            });
        }

        HttpContext.Session.SetObject("FoodCart", cart);
        TempData["Info"] = $"{item.Name} added to cart!";

        return RedirectToAction("Index");
    }

    // GET: Food/Cart
    [Authorize(Roles = "Member")]
    public IActionResult Cart()
    {
        var cart = HttpContext.Session.GetObject<List<CartItem>>("FoodCart") ?? new List<CartItem>();
        return View(cart);
    }

    // POST: Food/UpdateCart
    [Authorize(Roles = "Member")]
    [HttpPost]
    public IActionResult UpdateCart(string foodItemId, int quantity)
    {
        var cart = HttpContext.Session.GetObject<List<CartItem>>("FoodCart") ?? new List<CartItem>();
        var item = cart.FirstOrDefault(c => c.FoodItemId == foodItemId);

        if (item != null)
        {
            if (quantity > 0)
            {
                item.Quantity = quantity;
            }
            else
            {
                cart.Remove(item);
            }
        }

        HttpContext.Session.SetObject("FoodCart", cart);
        return RedirectToAction("Cart");
    }

    // POST: Food/RemoveFromCart
    [Authorize(Roles = "Member")]
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

    // POST: Food/ClearCart
    [Authorize(Roles = "Member")]
    [HttpPost]
    public IActionResult ClearCart()
    {
        HttpContext.Session.Remove("FoodCart");
        TempData["Info"] = "Cart cleared.";
        return RedirectToAction("Index");
    }

    // GET: Food/Checkout
    [Authorize(Roles = "Member")]
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
    [Authorize(Roles = "Member")]
    [HttpPost]
    public IActionResult PlaceOrder(string paymentMethod)
    {
        var cart = HttpContext.Session.GetObject<List<CartItem>>("FoodCart") ?? new List<CartItem>();

        if (!cart.Any())
        {
            TempData["Error"] = "Your cart is empty!";
            return RedirectToAction("Index");
        }

        // Get the actual User ID (not email)
        var userEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail))
        {
            TempData["Error"] = "User not authenticated.";
            return RedirectToAction("Login", "Account");
        }

        // Find user by email to get their ID
        var user = db.Users.FirstOrDefault(u => u.Email == userEmail);
        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("Login", "Account");
        }

        // Generate redemption code
        var redemptionCode = QRCodeHelper.GenerateRedemptionCode();

        // Create food order with redemption details
        var order = new FoodOrder
        {
            Id = Guid.NewGuid().ToString(),
            UserId = user.Id, // Use the actual user ID, not email
            OrderDateTime = DateTime.Now,
            TotalAmount = cart.Sum(c => c.Price * c.Quantity),
            Status = "Confirmed",
            PaymentMethod = paymentMethod,
            RedemptionCode = redemptionCode,
            ExpiresAt = DateTime.Now.AddHours(2), // Valid for 2 hours
            IsRedeemed = false
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

        // Check if there's a pending ticket booking
        var pendingShowtimeId = HttpContext.Session.GetString("PendingTicketShowtimeId");
        var pendingSeatIds = HttpContext.Session.GetObject<List<string>>("PendingTicketSeatIds");

        if (!string.IsNullOrEmpty(pendingShowtimeId) && pendingSeatIds != null && pendingSeatIds.Any())
        {
            // Create tickets
            var ticket = new Ticket
            {
                Id = Guid.NewGuid().ToString(),
                UserId = user.Id, // Use actual user ID
                ShowtimeId = pendingShowtimeId,
                BookingDateTime = DateTime.Now
            };
            db.Tickets.Add(ticket);

            foreach (var seatId in pendingSeatIds)
            {
                db.TicketSeats.Add(new TicketSeat
                {
                    Id = Guid.NewGuid().ToString(),
                    TicketId = ticket.Id,
                    SeatId = seatId
                });
            }

            // Clear pending ticket data
            HttpContext.Session.Remove("PendingTicketShowtimeId");
            HttpContext.Session.Remove("PendingTicketSeatIds");

            TempData["Info"] = "Tickets and food order placed successfully!";
        }
        else
        {
            TempData["Info"] = "Food order placed successfully!";
        }

        try
        {
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error placing order: {ex.Message}";
            return RedirectToAction("Cart");
        }

        // Clear cart
        HttpContext.Session.Remove("FoodCart");

        return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
    }

    // GET: Food/OrderConfirmation
    [Authorize(Roles = "Member")]
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

        // Generate QR code data (JSON format for easy parsing)
        var qrData = System.Text.Json.JsonSerializer.Serialize(new
        {
            OrderId = order.Id,
            RedemptionCode = order.RedemptionCode,
            Amount = order.TotalAmount,
            OrderDate = order.OrderDateTime.ToString("yyyy-MM-dd HH:mm:ss")
        });

        ViewBag.QRCodeBase64 = QRCodeHelper.GenerateQRCodeBase64(qrData);

        return View(order);
    }

    // GET: Food/Redeem - Staff redemption page
    [Authorize(Roles = "Admin")] // Only staff can access
    public IActionResult Redeem()
    {
        return View();
    }

    // POST: Food/RedeemOrder - Process redemption
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public IActionResult RedeemOrder(string redemptionCode)
    {
        var order = db.FoodOrders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.FoodItem)
            .FirstOrDefault(o => o.RedemptionCode == redemptionCode);

        if (order == null)
        {
            return Json(new { success = false, message = "Invalid redemption code." });
        }

        if (order.IsRedeemed)
        {
            return Json(new
            {
                success = false,
                message = $"Order already redeemed on {order.RedeemedAt:MMM dd, yyyy hh:mm tt}."
            });
        }

        if (order.ExpiresAt < DateTime.Now)
        {
            return Json(new
            {
                success = false,
                message = $"Order expired on {order.ExpiresAt:MMM dd, yyyy hh:mm tt}."
            });
        }

        // Mark as redeemed
        order.IsRedeemed = true;
        order.RedeemedAt = DateTime.Now;
        order.RedeemedBy = User.Identity?.Name;
        order.Status = "Redeemed";

        db.SaveChanges();

        return Json(new
        {
            success = true,
            message = "Order redeemed successfully!",
            order = new
            {
                orderId = order.Id,
                orderDate = order.OrderDateTime.ToString("MMM dd, yyyy hh:mm tt"),
                items = order.OrderItems.Select(i => new
                {
                    name = i.FoodItem.Name,
                    quantity = i.Quantity
                }),
                totalAmount = order.TotalAmount
            }
        });
    }

    // GET: Food/MyOrders - View user's order history
    [Authorize]
    public IActionResult MyOrders()
    {
        // Get the current user's email from Identity
        var userEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail))
        {
            TempData["Error"] = "User not authenticated.";
            return RedirectToAction("Login", "Account");
        }

        // Find user by email to get their ID
        var user = db.Users.FirstOrDefault(u => u.Email == userEmail);
        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("Login", "Account");
        }

        // Now query orders using the actual UserId
        var orders = db.FoodOrders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.FoodItem)
            .Where(o => o.UserId == user.Id) // Fixed: use user.Id instead of email
            .OrderByDescending(o => o.OrderDateTime)
            .ToList();

        return View(orders);
    }

    // GET: Food/OrderQR/{orderId} - View specific order QR code
    [Authorize]
    public IActionResult OrderQR(string orderId)
    {
        // Get the current user's email
        var userEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail))
        {
            TempData["Error"] = "User not authenticated.";
            return RedirectToAction("Login", "Account");
        }

        // Find user by email to get their ID
        var user = db.Users.FirstOrDefault(u => u.Email == userEmail);
        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("Login", "Account");
        }

        // Get the order
        var order = db.FoodOrders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.FoodItem)
            .FirstOrDefault(o => o.Id == orderId);

        // Check if order exists
        if (order == null)
        {
            TempData["Error"] = "Order not found.";
            return RedirectToAction("MyOrders");
        }

        // Check if user owns this order or is admin
        if (order.UserId != user.Id && !User.IsInRole("Admin"))
        {
            TempData["Error"] = "You don't have permission to view this order.";
            return RedirectToAction("MyOrders");
        }

        // Generate QR code data
        var qrData = System.Text.Json.JsonSerializer.Serialize(new
        {
            OrderId = order.Id,
            RedemptionCode = order.RedemptionCode,
            Amount = order.TotalAmount,
            OrderDate = order.OrderDateTime.ToString("yyyy-MM-dd HH:mm:ss")
        });

        ViewBag.QRCodeBase64 = QRCodeHelper.GenerateQRCodeBase64(qrData);

        return View(order);
    }

    [Authorize(Roles = "Member")]
    public IActionResult Select()
    {
        var foodItems = db.FoodItems
                          .Include(f => f.Category)
                          .ToList();

        ViewBag.Categories = db.FoodCategories.ToList();

        // Read session
        var selectedOrderIds = HttpContext.Session
                                          .GetObject<List<(string FoodId, int Quantity)>>("PendingOrderIds")
                                          ?? new List<(string, int)>();

        // Pass a dictionary to view for easier lookup
        ViewBag.SelectedQuantities = selectedOrderIds
                                     .Where(f => f.FoodId != null)
                                     .ToDictionary(f => f.FoodId, f => f.Quantity);

        return View(foodItems);
    }

    [HttpGet]
    public JsonResult GetFoodItemsByFilter(string? categoryId, string? search)
    {
        var items = db.FoodItems
            .Include(f => f.Category)
            .Where(f => f.IsAvailable)
            .AsQueryable();

        if (!string.IsNullOrEmpty(categoryId))
        {
            items = items.Where(f => f.CategoryId == categoryId);
        }

        if (!string.IsNullOrEmpty(search))
        {
            items = items.Where(f => f.Name.Contains(search));
        }

        var result = items
            .OrderBy(f => f.Name)
            .Select(f => new
            {
                id = f.Id,
                name = f.Name,
                description = f.Description,
                price = f.Price,
                image = f.Image,
                categoryName = f.Category.Name
            })
            .ToList();

        return Json(result);
    }

    [Authorize(Roles = "Member")]
    [HttpPost]
    public IActionResult Select(Dictionary<string, int> quantities)
    {
        var cart = new List<OrderCartItemVM>();

        foreach (var entry in quantities)
        {
            if (entry.Value > 0)
            {
                var food = db.FoodItems.FirstOrDefault(f => f.Id == entry.Key);
                if (food != null)
                {
                    cart.Add(new OrderCartItemVM
                    {
                        FoodId = food.Id,
                        Name = food.Name,
                        Price = food.Price,
                        Quantity = entry.Value
                    });
                }
            }
        }

        HttpContext.Session.SetObject("PendingOrderIds", cart);

        if (cart.Any())
        {
            var selectedNames = string.Join(", ", cart.Select(f => f.Name));
            TempData["Info"] = $"You've selected: {selectedNames}";
        }

        return RedirectToAction("Checkout", "Ticket");
    }
}