using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Main.Controllers;

public class AccountController(DB db,
                               Helper hp) : Controller
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
        // (1) Get user (admin or member) record based on email (PK)
        var u = db.Users.Find(vm.Email);

        // (2) Custom validation -> verify password
        
        if (u == null || !hp.VerifyPassword(u.Password, vm.Password))
        {
            ModelState.AddModelError("", "Login credentials not matched.");
        }

        if (ModelState.IsValid)
        {
            TempData["Info"] = "Login successfully.";

            // (3) Sign in
            hp.SignIn(u!.Email, u.Role, vm.RememberMe);

            // (4) Handle return URL
            if (string.IsNullOrEmpty(returnURL))
            {
                return RedirectToAction("Index", "Home");
            }
        }
        
        return View(vm);
    }

    // GET: Account/Logout
    public IActionResult Logout(string? returnURL)
    {
        TempData["Info"] = "Logout successfully.";

        // Sign out
        hp.SignOut();

        return RedirectToAction("Index", "Home");
    }

    // GET: Account/AccessDenied
    public IActionResult AccessDenied(string? returnURL)
    {
        return View();
    }



    // ------------------------------------------------------------------------
    // Others
    // ------------------------------------------------------------------------

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
        if (ModelState.IsValid("Email") &&
            db.Users.Any(u => u.Email == vm.Email))
        {
            ModelState.AddModelError("Email", "Duplicated Email.");
        }

        if (ModelState.IsValid("Photo"))
        {
            var err = hp.ValidatePhoto(vm.Image);
            if (err != "") ModelState.AddModelError("Photo", err);
        }
        
        if (ModelState.IsValid)
        {
            // Insert member
            db.Users.Add(new()
            {
                Email = vm.Email,
                Password = hp.HashPassword(vm.Password),
                Username = vm.Username,
                Image = hp.SavePhoto(vm.Image, "photos"),
            });
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
        // Get user (admin or member) record based on email (PK)
        var u = db.Users.Find(User.Identity!.Name);
        if (u == null) return RedirectToAction("Index", "Home");

        // If current password not matched
        if (!hp.VerifyPassword(u.Password, vm.Current))
        {
            ModelState.AddModelError("Current", "Current Password not matched.");
        }

        if (ModelState.IsValid)
        {
            // Update user password (hash)
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
        // Get member record based on email (PK)
        var m = db.Users.Find(User.Identity!.Name);
        if (m == null) return RedirectToAction("Index", "Home");

        var vm = new UpdateProfileVM
        {
            Email = m.Email,
            Username =  m.Username,
            ImageURL = m.Image,
        };

        return View(vm);
    }

    // POST: Account/UpdateProfile
    [Authorize(Roles = "Member")]
    [HttpPost]
    public IActionResult UpdateProfile(UpdateProfileVM vm)
    {
        // Get member record based on email (PK)
        var m = db.Users.Find(User.Identity!.Name);
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
        var u = db.Users.Find(vm.Email);

        if (u == null)
        {
            ModelState.AddModelError("Email", "Email not found.");
        }

        if (ModelState.IsValid)
        {
            // Generate random password
            string password = hp.RandomPassword();

            // Update user (admin or member) record
            u!.Password = hp.HashPassword(password);
            db.SaveChanges();

            // Send reset password email

            TempData["Info"] = $"Password reset to <b>{password}</b>.";
            return RedirectToAction();
        }

        return View();
    }
}
