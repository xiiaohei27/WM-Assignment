using Main.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Main.Controllers;

public class MovieController(DB db) : Controller
{
    public IActionResult Index(string genre)
    {
        ViewBag.Genres = db.Movies.Where(m => m.Showtimes.Any()).Select(m => m.Genre).Distinct(); // Distinct removes the duplicates.
        var m = db.Movies
              .Include(m => m.Showtimes)
              .Where(m =>
                     m.Showtimes.Any() &&
                     (m.Genre == genre || genre == null))
              .ToList();

        if (Request.IsAjax())
        {
            return PartialView("_Display", m);
        }

        return View(m);
    }

    // GET: Movie/Detail/(MovieId)
    [Authorize(Roles = "Member")]
    public IActionResult Detail(string id)
    {
        var movie = db.Movies.Find(id); // Fetch movie by id.
        if (movie == null) return RedirectToAction("Index", "Home"); // If not found, go back to Home/Index.

        return View(movie);
    }
}
