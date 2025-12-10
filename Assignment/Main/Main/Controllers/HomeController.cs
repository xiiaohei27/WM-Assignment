using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Main.Controllers;


public class HomeController(DB db) : Controller
{
    // GET: Home/Index
    public IActionResult Index()
    {
        // Get the first 1 featured movie (just as an example)
        var featured = db.Movies.OrderByDescending(m => m.ReleaseDate).FirstOrDefault();

        // Get 4 movies for "Now Showing"
        var nowShowing = db.Movies.OrderByDescending(m => m.ReleaseDate).Take(4).ToList();

        // Send them to the view
        ViewBag.Featured = featured;
        return View(nowShowing); // pass the list of movies as the model
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
