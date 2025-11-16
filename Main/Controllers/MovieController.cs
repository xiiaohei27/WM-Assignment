using Microsoft.AspNetCore.Mvc;

namespace Main.Controllers;

public class MovieController(DB db) : Controller
{
    public IActionResult Index(string genre)
    {
        ViewBag.Movies = db.Movies;
        var movies = db.Movies.Where(m => m.Genre == genre || genre == null).ToList();
        return View(movies);
    }

    // GET: Movie/Detail/(MovieId)
    public IActionResult Detail(int id)
    {
        var movie = db.Movies.Find(id); // Fetch movie by id
        if (movie == null) return RedirectToAction("Index", "Home"); // If not found, go back Home/Index.

        return View(movie); // Pass the movie object to the view.
    }
}
