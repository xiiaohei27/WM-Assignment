using Microsoft.AspNetCore.Mvc;

namespace Main.Controllers;

public class MovieController(DB db) : Controller
{
    public IActionResult Index(string genre)
    {
        ViewBag.Genres = db.Movies.Select(m => m.Genre).Distinct(); // Distinct removes the duplicates.
        var m = db.Movies.Where(m => m.Genre == genre || genre == null);
        return View(m);
    }

    // GET: Movie/Detail/(MovieId)
    public IActionResult Detail(int id)
    {
        var movie = db.Movies.Find(id); // Fetch movie by id.
        if (movie == null) return RedirectToAction("Index", "Home"); // If not found, go back to Home/Index.

        return View(movie);
    }
}
