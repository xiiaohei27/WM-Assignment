using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
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

            var userEmail = db.Users
                  .Where(u => u.Username == User.Identity.Name)
                  .Select(u => u.Email)
                  .FirstOrDefault();

            // 2. Build Stripe session
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                SuccessUrl = Url.Action("Success", "Ticket", null, Request.Scheme),
                CancelUrl = Url.Action("Cancel", "Ticket", null, Request.Scheme),
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
            TempData["Success"] = "Your payment was successful and tickets have been booked!";
            SaveBookingFromSession();
            return RedirectToAction("Index", "Home");
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
            var pendingOrderIds = HttpContext.Session
                                            .GetObject<List<OrderCartItemVM>>("PendingOrderIds")
                                            ?? new List<OrderCartItemVM>();

            if (string.IsNullOrEmpty(showtimeId) || !selectedSeatIds.Any())
                return;

            // 1. Create one ticket
            var ticket = new Ticket
            {
                Id = Guid.NewGuid().ToString(),
                UserId = User.Identity?.Name,
                ShowtimeId = showtimeId,
                BookingDateTime = DateTime.Now
            };
            db.Tickets.Add(ticket);

            // 2. Link all seats to this ticket
            foreach (var seatId in selectedSeatIds)
            {
                db.TicketSeats.Add(new TicketSeat
                {
                    Id = Guid.NewGuid().ToString(),
                    TicketId = ticket.Id,
                    SeatId = seatId
                });
            }

            // 3. Attach food
            foreach (var item in pendingOrderIds)
            {
                db.TicketFoods.Add(new TicketFood
                {
                    Id = Guid.NewGuid().ToString(),
                    TicketId = ticket.Id,
                    FoodItemId = item.FoodId,
                    Quantity = item.Quantity,
                    Redeemed = false    
                });
            }

            db.SaveChanges();

            // Clear sessions
            HttpContext.Session.Remove("PendingTicketShowtimeId");
            HttpContext.Session.Remove("PendingTicketSeatIds");
            HttpContext.Session.Remove("PendingOrderIds");
        }

        //[Authorize]
        public IActionResult History()
        {
            var tickets = db.Tickets
                            .Include(t => t.Showtime)
                                .ThenInclude(s => s.Movie)
                            .Include(t => t.Showtime)
                                .ThenInclude(s => s.Hall)
                                .ThenInclude(h => h.Cinema)
                            .Include(t => t.TicketSeats)
                                .ThenInclude(ts => ts.Seat)
                            .Include(t => t.TicketFoods)
                                .ThenInclude(tf => tf.FoodItem)
                            .Where(t => t.UserId == User.Identity.Name)
                            .OrderByDescending(t => t.BookingDateTime)
                            .Select(t => new BookingHistoryVM
                            {
                                TicketId = t.Id,
                                MovieTitle = t.Showtime.Movie.Title,
                                CinemaName = t.Showtime.Hall.Cinema.Name,
                                HallName = t.Showtime.Hall.Name,
                                SeatNumbers = t.TicketSeats.Select(ts => ts.Seat.SeatNumber).ToList(),
                                Showtime = t.Showtime.StartDateTime,
                                BookingDate = t.BookingDateTime,
                                FoodItems = t.TicketFoods.Select(tf => tf.FoodItem.Name).ToList(),
                                QRCodeData = $"Ticket:{t.Id};Movie:{t.Showtime.Movie.Title};Seats:{string.Join(',', t.TicketSeats.Select(ts => ts.Seat.SeatNumber))};Cinema:{t.Showtime.Hall.Cinema.Name};Time:{t.Showtime.StartDateTime}"
                            })
                            .ToList();

            return View(tickets);
        }

        public IActionResult QRCode(string id)
        {
            var ticket = db.Tickets
                           .Include(t => t.Showtime)
                               .ThenInclude(s => s.Movie)
                           .Include(t => t.Showtime)
                               .ThenInclude(s => s.Hall)
                               .ThenInclude(h => h.Cinema)
                           .Include(t => t.TicketSeats)
                                .ThenInclude(ts => ts.Seat)
                           .FirstOrDefault(t => t.Id == id);

            if (ticket == null) return NotFound();

            var seatNumbers = string.Join(',', ticket.TicketSeats.Select(ts => ts.Seat.SeatNumber));
            var qrText = $"Ticket:{ticket.Id};Movie:{ticket.Showtime.Movie.Title};Seats:{seatNumbers};Cinema:{ticket.Showtime.Hall.Cinema.Name};Time:{ticket.Showtime.StartDateTime}";

            // Use PngByteQRCode
            using (var qrGenerator = new QRCodeGenerator())
            using (var qrData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q))
            {
                var qrCode = new PngByteQRCode(qrData);
                byte[] qrBytes = qrCode.GetGraphic(20); // 20 = pixels per module
                return File(qrBytes, "image/png");
            }
        }

        public IActionResult Detail(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var ticket = db.Tickets
                           .Include(t => t.Showtime)
                               .ThenInclude(s => s.Movie)
                           .Include(t => t.Showtime)
                               .ThenInclude(s => s.Hall)
                               .ThenInclude(h => h.Cinema)
                           .Include(t => t.TicketSeats)
                                .ThenInclude(ts => ts.Seat)
                           .Include(t => t.TicketFoods)
                               .ThenInclude(tf => tf.FoodItem)
                           .FirstOrDefault(t => t.Id == id && t.UserId == User.Identity.Name);

            if (ticket == null)
                return NotFound();

            var vm = new BookingHistoryVM
            {
                TicketId = ticket.Id,
                MovieTitle = ticket.Showtime.Movie.Title,
                CinemaName = ticket.Showtime.Hall.Cinema.Name,
                HallName = ticket.Showtime.Hall.Name,
                SeatNumbers = ticket.TicketSeats.Select(ts => ts.Seat.SeatNumber).ToList(),
                Showtime = ticket.Showtime.StartDateTime,
                BookingDate = ticket.BookingDateTime,
                FoodItems = ticket.TicketFoods.Select(tf => tf.FoodItem.Name).ToList(),
                QRCodeData = $"Ticket:{ticket.Id};Movie:{ticket.Showtime.Movie.Title};Seats:{string.Join(',', ticket.TicketSeats.Select(ts => ts.Seat.SeatNumber))};Cinema:{ticket.Showtime.Hall.Cinema.Name};Time:{ticket.Showtime.StartDateTime}"
            };

            return View(vm);
        }
    }
}
