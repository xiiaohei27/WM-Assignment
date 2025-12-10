using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Main.Controllers;

public class AccountController(DB db, Helper hp) : Controller
{
    // GET: Account/Login
    public IActionResult Login()
    {
        return View();
    }

    // POST: Account/Login
    [HttpPost]
    public IActionResult Login(LoginVM vm, string? returnURL)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var Email = vm.Email?.Trim();
        if (string.IsNullOrEmpty(Email))
        {
            ModelState.AddModelError("Email", "Email is required.");
            return View(vm);
        }

        // FIX: Use FirstOrDefault to query by Email
        var u = db.Users.FirstOrDefault(u => u.Email == Email);
        if (u == null)
        {
            ModelState.AddModelError("", "Login credentials not matched.");
            return View(vm);
        }

        if (!hp.VerifyPassword(u.Password, vm.Password))
        {
            ModelState.AddModelError("", "Login credentials not matched.");
            return View(vm);
        }

        // FIX: Get the role from the discriminator (type name)
        string role = u.GetType().Name; // Returns "Admin" or "Member"

        TempData["Info"] = "Login successfully.";
        hp.SignIn(u.Email, role, vm.RememberMe);

        if (!string.IsNullOrEmpty(returnURL) && Url.IsLocalUrl(returnURL))
        {
            return LocalRedirect(returnURL);
        }

        return RedirectToAction("Index", "Home");
    }

    // GET: Account/Logout
    public IActionResult Logout(string? returnURL)
    {
        TempData["Info"] = "Logout successfully.";
        hp.SignOut();
        return RedirectToAction("Index", "Home");
    }

    // GET: Account/AccessDenied
    public IActionResult AccessDenied(string? returnURL)
    {
        return View();
    }

    // GET: Account/CheckEmail
    public bool CheckEmail(string email)
    {
        return !db.Users.Any(u => u.Email == email);
    }

    // GET: Account/Register
    public IActionResult Register()
    {
        return View();
    }

    // POST: Account/Register
    [HttpPost]
    public IActionResult Register(RegisterVM vm)
    {
        if (ModelState.GetFieldValidationState("Email") != ModelValidationState.Invalid &&
            db.Users.Any(u => u.Email == vm.Email))
        {
            ModelState.AddModelError("Email", "Duplicated Email.");
        }

        if (ModelState.GetFieldValidationState("Image") != ModelValidationState.Invalid)
        {
            var err = hp.ValidatePhoto(vm.Image);
            if (err != "") ModelState.AddModelError("Image", err);
        }

        if (ModelState.IsValid)
        {
            // Create a new Member (not Admin)
            var member = new Member()
            {
                Id = Guid.NewGuid().ToString(),
                Email = vm.Email,
                Password = hp.HashPassword(vm.Password),
                Username = vm.Username,
                Image = hp.SavePhoto(vm.Image, "photos"),
            };

            db.Users.Add(member);
            db.SaveChanges();

            TempData["Info"] = "Register successfully. Please login.";
            return RedirectToAction("Login");
        }

        return View(vm);
    }

    // GET: Account/UpdatePassword
    [Authorize]
    public IActionResult UpdatePassword()
    {
        return View();
    }

    // POST: Account/UpdatePassword
    [Authorize]
    [HttpPost]
    public IActionResult UpdatePassword(UpdatePasswordVM vm)
    {
        // FIX: Find by Email (User.Identity.Name), not by Id
        var u = db.Users.FirstOrDefault(u => u.Email == User.Identity!.Name);
        if (u == null) return RedirectToAction("Index", "Home");

        if (!hp.VerifyPassword(u.Password, vm.Current))
        {
            ModelState.AddModelError("Current", "Current Password not matched.");
        }

        if (ModelState.IsValid)
        {
            u.Password = hp.HashPassword(vm.New);
            db.SaveChanges();

            TempData["Info"] = "Password updated.";
            return RedirectToAction();
        }

        return View();
    }

    // GET: Account/UpdateProfile
    [Authorize(Roles = "Member")]
    public IActionResult UpdateProfile()
    {
        // FIX: Find by Email
        var m = db.Users.FirstOrDefault(u => u.Email == User.Identity!.Name);
        if (m == null) return RedirectToAction("Index", "Home");

        var vm = new UpdateProfileVM
        {
            Email = m.Email,
            Username = m.Username,
            ImageURL = m.Image,
        };

        return View(vm);
    }

    // POST: Account/UpdateProfile
    [Authorize(Roles = "Member")]
    [HttpPost]
    public IActionResult UpdateProfile(UpdateProfileVM vm)
    {
        // FIX: Find by Email
        var m = db.Users.FirstOrDefault(u => u.Email == User.Identity!.Name);
        if (m == null) return RedirectToAction("Index", "Home");

        if (vm.Image != null)
        {
            var err = hp.ValidatePhoto(vm.Image);
            if (err != "") ModelState.AddModelError("Photo", err);
        }

        if (ModelState.IsValid)
        {
            m.Username = vm.Username;

            if (vm.Image != null)
            {
                hp.DeletePhoto(m.Image, "photos");
                m.Image = hp.SavePhoto(vm.Image, "photos");
            }

            db.SaveChanges();

            TempData["Info"] = "Profile updated.";
            return RedirectToAction();
        }

        vm.Email = m.Email;
        vm.ImageURL = m.Image;
        return View(vm);
    }

    // GET: Account/ResetPassword
    public IActionResult ResetPassword()
    {
        return View();
    }

    // POST: Account/ResetPassword
    [HttpPost]
    public IActionResult ResetPassword(ResetPasswordVM vm)
    {
        // FIX: Find by Email, not by Id
        var u = db.Users.FirstOrDefault(u => u.Email == vm.Email);

        if (u == null)
        {
            ModelState.AddModelError("Email", "Email not found.");
        }

        if (ModelState.IsValid)
        {
            string password = hp.RandomPassword();
            u!.Password = hp.HashPassword(password);
            db.SaveChanges();

            TempData["Info"] = $"Password reset to <b>{password}</b>.";
            return RedirectToAction();
        }

        return View();
    }
}