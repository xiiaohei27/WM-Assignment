using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace Main.Controllers
{
    public class TicketController(DB db) : Controller
    {
        // Show checkout page
        public IActionResult Checkout()
        {
            // 1. Get pending showtime & seats
            var showtimeId = HttpContext.Session.GetString("PendingTicketShowtimeId");
            var selectedSeatIds = HttpContext.Session
                                      .GetObject<List<string>>("PendingTicketSeatIds")
                                      ?? new List<string>();

            if (string.IsNullOrEmpty(showtimeId) || !selectedSeatIds.Any())
            {
                TempData["Error"] = "No seats selected.";
                return RedirectToAction("Index", "Movie");
            }

            var showtime = db.Showtimes
                             .Include(s => s.Movie)
                             .Include(s => s.Hall)
                             .ThenInclude(h => h.Cinema)
                             .FirstOrDefault(s => s.Id == showtimeId);

            if (showtime == null)
                return RedirectToAction("Index", "Movie");

            var seats = db.Seats
                          .Where(s => selectedSeatIds.Contains(s.Id))
                          .ToList();

            // 2. Get pending food & beverage orders (from session)
            var foodCart = HttpContext.Session
                                     .GetObject<List<OrderCartItemVM>>("PendingOrderIds")
                                     ?? new List<OrderCartItemVM>();

            // 3. Build the ViewModel
            var vm = new TicketCheckoutVM
            {
                Showtime = showtime,
                Seats = seats,
                FoodCart = foodCart
            };

            return View(vm);
        }

        [HttpPost]
        public IActionResult ProcessCheckout()
        {
            // 1. Get session data
            var showtimeId = HttpContext.Session.GetString("PendingTicketShowtimeId");
            var selectedSeatIds = HttpContext.Session
                                      .GetObject<List<string>>("PendingTicketSeatIds")
                                      ?? new List<string>();
            var pendingOrderIds = HttpContext.Session
                                           .GetObject<List<OrderCartItemVM>>("PendingOrderIds")
                                           ?? new List<OrderCartItemVM>();

            if (string.IsNullOrEmpty(showtimeId) || !selectedSeatIds.Any())
            {
                TempData["Error"] = "No seats selected.";
                return RedirectToAction("Index", "Movie");
            }

            var showtime = db.Showtimes
                             .Include(s => s.Movie)
                             .Include(s => s.Hall)
                             .ThenInclude(h => h.Cinema)
                             .FirstOrDefault(s => s.Id == showtimeId);

            if (showtime == null)
                return RedirectToAction("Index", "Movie");

            var seats = db.Seats
                          .Where(s => selectedSeatIds.Contains(s.Id))
                          .ToList();

            if (!seats.Any() && !pendingOrderIds.Any())
            {
                TempData["Error"] = "No items to pay for.";
                return RedirectToAction("Checkout");
            }

            // 2. Build Stripe session
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                SuccessUrl = Url.Action("Success", "Ticket", null, Request.Scheme),
                CancelUrl = Url.Action("Cancel", "Ticket", null, Request.Scheme)
            };

            // Add seats to Stripe
            decimal ticketPrice = showtime.TicketPrice;
            foreach (var seat in seats)
            {
                options.LineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "myr",
                        UnitAmountDecimal = ticketPrice * seat.Multiplier * 100,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"{showtime.Movie.Title} - Seat {seat.SeatNumber} ({seat.SeatType})",
                            Description = $"{showtime.Hall.Cinema.Name} - {showtime.Hall.Name}, {showtime.StartDateTime:ddd, MMM dd yyyy hh:mm tt}"
                        }
                    },
                    Quantity = 1
                });
            }

            // Add food & beverages to Stripe
            foreach (var item in pendingOrderIds)
            {
                options.LineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "myr",
                        UnitAmountDecimal = item.Price * 100,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Name,
                            Description = item.Name
                        }
                    },
                    Quantity = item.Quantity
                });
            }

            var service = new SessionService();
            Session session = service.Create(options);

            return Redirect(session.Url);
        }

        public IActionResult Success()
        {
            SaveBookingFromSession();
            return View();
        }

        public IActionResult Cancel()
        {
            TempData["Error"] = "Payment canceled.";
            return RedirectToAction("Checkout");
        }

        // Save tickets & orders
        private void SaveBookingFromSession()
        {
            var showtimeId = HttpContext.Session.GetString("PendingTicketShowtimeId");
            var selectedSeatIds = HttpContext.Session
                                      .GetObject<List<string>>("PendingTicketSeatIds")
                                      ?? new List<string>();

            // Save tickets
            if (!string.IsNullOrEmpty(showtimeId) && selectedSeatIds.Any())
            {
                foreach (var seatId in selectedSeatIds)
                {
                    db.Tickets.Add(new Ticket
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = User.Identity?.Name,
                        ShowtimeId = showtimeId,
                        SeatId = seatId,
                        BookingDateTime = DateTime.Now
                    });
                }
            }

            var pendingOrderIds = HttpContext.Session
                                           .GetObject<List<OrderCartItemVM>>("PendingOrderIds")
                                           ?? new List<OrderCartItemVM>();

            // Save food & beverage orders
            if (pendingOrderIds.Any())
            {
                var order = new FoodOrder
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = User.Identity?.Name,
                    OrderDateTime = DateTime.Now,
                    TotalAmount = pendingOrderIds.Sum(p => p.Price * p.Quantity),
                    Status = "Confirmed"
                };

                db.FoodOrders.Add(order);

                foreach (var item in pendingOrderIds)
                {
                    db.OrderItems.Add(new OrderItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        FoodOrderId = order.Id,
                        FoodItemId = item.FoodId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price,
                        Subtotal = item.Price * item.Quantity
                    });
                }
            }

            db.SaveChanges();

            // Clear sessions
            HttpContext.Session.Remove("PendingTicketShowtimeId");
            HttpContext.Session.Remove("PendingTicketSeatIds");
            HttpContext.Session.Remove("PendingOrderIds");
        }
    }
}
