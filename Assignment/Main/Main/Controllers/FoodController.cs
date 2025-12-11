// POST: Food/PlaceOrder
using Main;
using Main.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[HttpPost]
public IActionResult PlaceOrder(string paymentMethod)
{
    var cart = HttpContext.Session.GetObject<List<CartItem>>("FoodCart") ?? new List<CartItem>();

    if (!cart.Any())
    {
        TempData["Error"] = "Your cart is empty!";
        return RedirectToAction("Index");
    }

    // Generate redemption code
    var redemptionCode = QRCodeHelper.GenerateRedemptionCode();

    // Create food order with redemption details
    var order = new FoodOrder
    {
        Id = Guid.NewGuid().ToString(),
        UserId = User.Identity?.IsAuthenticated == true ? User.Identity.Name : null,
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
        foreach (var seatId in pendingSeatIds)
        {
            var ticket = new Ticket
            {
                Id = Guid.NewGuid().ToString(),
                UserId = User.Identity?.IsAuthenticated == true ? User.Identity.Name : null,
                ShowtimeId = pendingShowtimeId,
                SeatId = seatId,
                BookingDateTime = DateTime.Now
            };
            db.Tickets.Add(ticket);
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

    db.SaveChanges();

    // Clear cart
    HttpContext.Session.Remove("FoodCart");

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
    var orders = db.FoodOrders
        .Include(o => o.OrderItems)
        .ThenInclude(oi => oi.FoodItem)
        .Where(o => o.UserId == User.Identity!.Name)
        .OrderByDescending(o => o.OrderDateTime)
        .ToList();

    return View(orders);
}

// GET: Food/OrderQR/{orderId} - View specific order QR code
[Authorize]
public IActionResult OrderQR(string orderId)
{
    var order = db.FoodOrders
        .Include(o => o.OrderItems)
        .ThenInclude(oi => oi.FoodItem)
        .FirstOrDefault(o => o.Id == orderId);

    if (order == null || (order.UserId != User.Identity!.Name && !User.IsInRole("Admin")))
    {
        TempData["Error"] = "Order not found.";
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