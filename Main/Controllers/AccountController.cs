using Microsoft.AspNetCore.Mvc;

namespace Main.Controllers;

public class AccountController(DB db) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        // Check user in database
        var user = db.Users.FirstOrDefault(u => u.Username == username && u.Password == password);

        if (user != null)
        {
            // TODO: Add authentication/session logic
            ViewBag.Message = $"Welcome, {user.Username}!";
            return RedirectToAction("Index", "Home");
        }
        else
        {
            ViewBag.Error = "Invalid username or password.";
            return View();
        }
    }

    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Register(string username, string password, string role)
    {
        // Simple registration logic
        var existingUser = db.Users.FirstOrDefault(u => u.Username == username);
        if (existingUser != null)
        {
            ViewBag.Error = "Username already exists!";
            return View();
        }

        var newUser = new User
        {
            Username = username,
            Password = password, // No need to have any constraint or?
            Role = "Customer"
        };

        db.Users.Add(newUser);
        db.SaveChanges();

        ViewBag.Message = "Registration successful!";
        return RedirectToAction("Login");
    }
    public IActionResult Logout()
    {
        // TODO: Clear session/authentication here later, idk how to right now xd.
        ViewBag.Message = "You have been logged out (placeholder).";

        return RedirectToAction("Index", "Home");
    }
}
