using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Main.Controllers;

[Authorize(Roles = "Member")]
public class CinemaController(DB db) : Controller
{
    // Step 1: List cinemas for a movie
    public IActionResult SelectCinema(string movieId)
    {
        var movie = db.Movies.Find(movieId);
        if (movie == null) return RedirectToAction("Index", "Movie");

        var cinemas = db.Showtimes
                        .Where(s => s.MovieId == movieId)
                        .Select(s => s.Hall.Cinema)
                        .Distinct()
                        .ToList();

        ViewBag.Movie = movie;
        return View(cinemas);
    }
}
