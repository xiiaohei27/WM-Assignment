using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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
        // 1. Validate incoming model
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        // 2. Normalize and validate email
        var Email = vm.Email?.Trim();
        if (string.IsNullOrEmpty(Email))
        {
            ModelState.AddModelError("Email", "Email is required.");
            return View(vm);
        }

        // 3. Retrieve by primary key (Email)
        var u = db.Users.FirstOrDefault(u => u.Email == Email);
        if (u == null)
        {
            // 4. User not found
            ModelState.AddModelError("", "Login credentials not matched.");
            return View(vm);
        }

        // 5. Verify password
        if (!hp.VerifyPassword(u.Password, vm.Password))
        {
            ModelState.AddModelError("", "Login credentials not matched.");
            return View(vm);
        }

        // 6. Successful sign-in and redirect handling
        TempData["Info"] = "Login successfully.";
        hp.SignIn(u.Email, u.Role, vm.RememberMe);

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
        // Replace IsValidField with GetFieldValidationState check
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
            // Insert member
            db.Users.Add(new Member()  // or new Admin()
            {
                Id = Guid.NewGuid().ToString(),
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
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        // Get user (admin or member) record based on email (PK)
        var u = db.Users.Find(User.Identity!.Name);
        if (u == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("Index", "Home");
        }

        // If current password not matched
        if (!hp.VerifyPassword(u.Password, vm.Current))
        {
            ModelState.AddModelError("Current", "Current password is incorrect.");
            return View(vm);
        }

        // Check if new password is same as current password
        if (hp.VerifyPassword(u.Password, vm.New))
        {
            ModelState.AddModelError("New", "New password must be different from current password.");
            return View(vm);
        }

        // Update user password (hash)
        u.Password = hp.HashPassword(vm.New);
        db.SaveChanges();

        TempData["Info"] = "Password updated successfully.";
        return RedirectToAction();
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
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var u = db.Users.FirstOrDefault(u => u.Email == vm.Email);

        if (u == null)
        {
            ModelState.AddModelError("Email", "Email not found.");
            return View(vm);
        }

        // Generate random password
        string password = hp.RandomPassword();

        // Update user (admin or member) record
        u.Password = hp.HashPassword(password);
        db.SaveChanges();

        // TODO: Send reset password email in production
        // For now, display the password (NOT RECOMMENDED IN PRODUCTION)
        TempData["Info"] = $"Password has been reset. Your new password is: <b>{password}</b><br/>Please login and change your password immediately.";

        return RedirectToAction("Login");
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

}