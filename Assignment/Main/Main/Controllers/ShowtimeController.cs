using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Main.Controllers;

[Authorize(Roles = "Member")]
public class ShowtimeController(DB db) : Controller
{
    // Step 2: Select available dates for a movie at a cinema
    public IActionResult SelectDate(string movieId)
    {
        var movie = db.Movies.Find(movieId);
        if (movie == null) return RedirectToAction("Index", "Movie");

        var dates = db.Showtimes
                      .Where(s => s.MovieId == movieId)
                      .Select(s => s.StartDateTime.Date)
                      .Distinct()
                      .ToList();

        ViewBag.Movie = movie;
        return View(dates);
    }

    // Step 3: Select showtime for a movie at a cinema on a specific date
    public IActionResult SelectShowtime(string movieId, DateTime date)
    {
        var movie = db.Movies.Find(movieId);
        if (movie == null) return RedirectToAction("Index", "Movie");

        var showtimes = db.Showtimes
                          .Where(s => s.MovieId == movieId
                          && s.StartDateTime.Date == date)
                          .Include(s => s.Hall)
                          .ToList();

        ViewBag.Movie = movie;
        ViewBag.Date = date;
        return View(showtimes);
    }
}
