using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Main.Controllers;


public class HomeController(DB db) : Controller
{
    // GET: Home/Index
    public IActionResult Index()
    {
        if (User.IsInRole("Admin"))
        {
            return RedirectToAction("Dashboard", "Admin");
        }

        // Get the first featured movie that has at least one showtime
        var featured = db.Movies
                         .Include(m => m.Showtimes) // include showtimes
                         .Where(m => m.Showtimes.Any(s => s.StartDateTime >= DateTime.Now)) // Only movies with showtimes with the StartDateTime later than now.
                         .OrderByDescending(m => m.ReleaseDate)
                         .FirstOrDefault();

        // Get movies for "Now Showing" that have showtimes
        var nowShowing = db.Movies
                           .Include(m => m.Showtimes)
                           .Where(m => m.Showtimes.Any(s => s.StartDateTime >= DateTime.Now))
                           .OrderByDescending(m => m.ReleaseDate)
                           .ToList();

        // Send them to the view
        ViewBag.Featured = featured;
        return View(nowShowing); // Pass the list of movies as the model.
    }

    // GET: Home/Both
    [Authorize]
    public IActionResult Both()
    {
        return View();
    }

    // GET: Home/Member
    [Authorize(Roles = "Member")]
    public IActionResult Member()
    {
        return View();
    }

    // GET: Home/Admin
    [Authorize(Roles = "Admin")]
    public IActionResult Admin()
    {
        return View();
    }
}
