using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Main.Models;

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
        if (!ModelState.IsValid) return View(vm);

        // (1) Get user by email (not by PK)
        var u = db.Users.SingleOrDefault(x => x.Email == vm.Email);

        // (2) Verify password using the mapped Password property
        if (u == null || !hp.VerifyPassword(u.Password, vm.Password))
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(vm);
        }

        TempData["Info"] = "Login successfully.";

        // (3) Sign in (Role is an enum -> ToString())
        hp.SignIn(u.Email, u.Role.ToString(), vm.RememberMe);

        // (4) Handle return URL safely
        if (!string.IsNullOrEmpty(returnURL) && Url.IsLocalUrl(returnURL))
        {
            return Redirect(returnURL);
        }

        return RedirectToAction("Index", "Home");
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
        // Check email validity first, then duplication
        if (ModelState.TryGetValue(nameof(vm.Email), out var _ms) &&
            _ms.Errors.Count == 0 &&
            db.Users.Any(u => u.Email == vm.Email))
        {
            ModelState.AddModelError(nameof(vm.Email), "Duplicated Email.");
        }

        // Validate photo if provided
        if (vm.Image != null)
        {
            var err = hp.ValidatePhoto(vm.Image);
            if (!string.IsNullOrEmpty(err)) ModelState.AddModelError(nameof(vm.Image), err);
        }

        if (ModelState.IsValid)
        {
            // Insert user — set Id, persist hashed password into the mapped Password property, set Role
            db.Users.Add(new()
            {
                Id = Guid.NewGuid().ToString("n"),
                Email = vm.Email,
                Password = hp.HashPassword(vm.Password),
                Username = vm.Name,
                Image = vm.Image != null ? hp.SavePhoto(vm.Image, "photos") : null,
                Role = UserRole.Member
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
        // find user by email (Name claim contains email)
        var u = db.Users.SingleOrDefault(x => x.Email == User.Identity!.Name);
        if (u == null) return RedirectToAction("Index", "Home");

        // If current password not matched
        if (!hp.VerifyPassword(u.Password, vm.Current))
        {
            ModelState.AddModelError(nameof(vm.Current), "Current password is incorrect.");
        }

        if (ModelState.IsValid)
        {
            // Update persisted password property
            u.Password = hp.HashPassword(vm.New);
            db.SaveChanges();

            TempData["Info"] = "Password updated.";
            return RedirectToAction();
        }

        return View(vm);
    }

    // GET: Account/UpdateProfile
    [Authorize(Roles = "Member")]
    public IActionResult UpdateProfile()
    {
        var m = db.Users.SingleOrDefault(x => x.Email == User.Identity!.Name);
        if (m == null) return RedirectToAction("Index", "Home");

        var vm = new UpdateProfileVM
        {
            Email = m.Email,
            Name =  m.Username,
            ImageURL = m.Image,
        };

        return View(vm);
    }

    // POST: Account/UpdateProfile
    [Authorize(Roles = "Member")]
    [HttpPost]
    public IActionResult UpdateProfile(UpdateProfileVM vm)
    {
        var m = db.Users.SingleOrDefault(x => x.Email == User.Identity!.Name);
        if (m == null) return RedirectToAction("Index", "Home");

        if (vm.Image != null)
        {
            var err = hp.ValidatePhoto(vm.Image);
            if (!string.IsNullOrEmpty(err)) ModelState.AddModelError(nameof(vm.Image), err);
        }

        if (ModelState.IsValid)
        {
            m.Username = vm.Name;

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
        var u = db.Users.SingleOrDefault(x => x.Email == vm.Email);

        if (u == null)
        {
            ModelState.AddModelError(nameof(vm.Email), "Email not found.");
        }

        if (ModelState.IsValid)
        {
            // Generate random password
            string password = hp.RandomPassword();

            // Update persisted password property
            u!.Password = hp.HashPassword(password);
            db.SaveChanges();

            // Send reset password email (TODO)

            TempData["Info"] = $"Password reset to <b>{password}</b>.";
            return RedirectToAction();
        }

        return View(vm);
    }
}
