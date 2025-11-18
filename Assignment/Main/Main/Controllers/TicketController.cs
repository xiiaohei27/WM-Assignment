using Microsoft.AspNetCore.Mvc;

namespace Main.Controllers;

public class TicketController(DB db) : Controller
{
    // GET: /Ticket/Buy/(MovieId)
    public IActionResult Buy(int movieId)
    {
        var movie = db.Movies.Find(movieId);
        if (movie == null) return RedirectToAction("Index", "Home");

        var showtimes = db.Showtimes.Where(s => s.MovieId == movieId).ToList();
        ViewBag.Movie = movie;

        return View(showtimes);
    }
}
