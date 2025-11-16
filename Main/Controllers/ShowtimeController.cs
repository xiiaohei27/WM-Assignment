using Microsoft.AspNetCore.Mvc;

namespace Demo.Controllers;

public class ShowtimeController(DB db) : Controller
{
    // GET: /Showtime/Seats/(ShowtimeId)
    public IActionResult Seats(int id) // id = ShowtimeId
    {
        var showtime = db.Showtimes.Find(id);
        if (showtime == null) return RedirectToAction("Index", "Home");

        // Get the movie separately
        var movie = db.Movies.Find(showtime.MovieId);

        // Generate seat numbers
        var seats = Enumerable.Range(1, showtime.TotalSeats).ToList();

        ViewBag.Showtime = showtime;
        ViewBag.Movie = movie; // pass movie separately
        ViewBag.Title = $"Seats for {showtime.Movie.Title} at {showtime.StartTime}";
        return View(seats);
    }
}
