using Main.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Main.Controllers;

public class SeatController(DB db) : Controller
{
    // Display seats for a specific showtime
    public IActionResult SelectSeats(string showtimeId)
    {
        var showtime = db.Showtimes
                        .Include(s => s.Movie)
                        .Include(s => s.Hall)
                        .ThenInclude(h => h.Seats)
                        .Include(s => s.Hall)
                        .ThenInclude(h => h.Cinema)
                        .FirstOrDefault(s => s.Id == showtimeId);

        if (showtime == null) return RedirectToAction("Index", "Movie");

        // All seats in the hall
        var allSeats = showtime.Hall.Seats.OrderBy(s => s.SeatNumber).ToList();

        // Seats already booked for this showtime
        var bookedSeats = db.Tickets
                            .Where(t => t.ShowtimeId == showtimeId)
                            .Select(t => t.SeatId)
                            .ToList();

        ViewBag.Showtime = showtime;
        ViewBag.BookedSeats = bookedSeats;

        return View(allSeats);
    }

    [HttpPost]
    public IActionResult SelectSeats(string showtimeId, List<string> selectedSeatIds)
    {
        if (selectedSeatIds == null || !selectedSeatIds.Any())
        {
            TempData["Error"] = "Please select at least one seat.";
            return RedirectToAction("SelectSeats", new { showtimeId });
        }

        // Store pending ticket information in session
        HttpContext.Session.SetString("PendingTicketShowtimeId", showtimeId);
        HttpContext.Session.SetObject("PendingTicketSeatIds", selectedSeatIds);

        TempData["Info"] = $"You've selected {selectedSeatIds.Count} seat(s). Please order food & beverages to complete your booking.";

        // Redirect to food ordering
        return RedirectToAction("Select", "Food");
    }
}