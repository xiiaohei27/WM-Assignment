using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Main.Controllers;

public class AccountController(DB db, Helper hp, IEmailService emailService) : Controller
{
    // GET: Account/Login
    public IActionResult Login()
    {
        if (User.Identity.IsAuthenticated)
        {
            // Redirect to home/dashboard if already logged in
            return RedirectToAction("Index", "Home");
        }
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

        // Check if email is verified
        if (!u.IsEmailVerified)
        {
            ViewBag.Email = u.Email;
            return View("EmailNotVerified");
        }

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
        // Clear shopping cart on logout
        HttpContext.Session.Remove("FoodCart");

        // Clear any pending data
        HttpContext.Session.Remove("PendingTicketShowtimeId");
        HttpContext.Session.Remove("PendingTicketSeatIds");
        HttpContext.Session.Remove("PendingOrderIds");

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
    public async Task<IActionResult> Register(RegisterVM vm)
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
            // Create new member
            var newUser = new Member()
            {
                Id = Guid.NewGuid().ToString(),
                Email = vm.Email,
                Password = hp.HashPassword(vm.Password),
                Username = vm.Username,
                Image = hp.SavePhoto(vm.Image, "photos"),
                IsEmailVerified = false
            };

            db.Users.Add(newUser);
            db.SaveChanges();

            // Create verification token
            var token = Guid.NewGuid().ToString();
            var verificationToken = new EmailVerificationToken
            {
                Id = Guid.NewGuid().ToString(),
                UserId = newUser.Id,
                Token = token,
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddHours(24),
                IsUsed = false
            };

            db.EmailVerificationTokens.Add(verificationToken);
            db.SaveChanges();

            // Generate verification link
            var verificationLink = Url.Action(
                "VerifyEmail",
                "Account",
                new { token = token },
                protocol: Request.Scheme
            );

            // Send verification email
            try
            {
                await emailService.SendVerificationEmailAsync(vm.Email, verificationLink!);
                TempData["Info"] = "Registration successful! Please check your email to verify your account.";
            }
            catch (Exception ex)
            {
                // For development: Show link if email fails
                TempData["Info"] = $"Registration successful! Click here to verify: <a href='{verificationLink}'>Verify Email</a>";
            }

            return View("RegisterSuccess");
        }

        return View(vm);
    }

    // GET: Account/VerifyEmail
    public IActionResult VerifyEmail(string token)
    {
        var verificationToken = db.EmailVerificationTokens
            .FirstOrDefault(t => t.Token == token && !t.IsUsed && t.ExpiresAt > DateTime.Now);

        if (verificationToken == null)
        {
            TempData["Error"] = "Invalid or expired verification link.";
            return RedirectToAction("Login");
        }

        var user = db.Users.Find(verificationToken.UserId);
        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("Login");
        }

        // Mark user as verified
        user.IsEmailVerified = true;
        user.EmailVerifiedAt = DateTime.Now;

        // Mark token as used
        verificationToken.IsUsed = true;
        verificationToken.UsedAt = DateTime.Now;

        db.SaveChanges();

        TempData["Info"] = "Email verified successfully! You can now login.";
        return View("VerificationSuccess");
    }

    // GET: Account/ResendVerification
    public IActionResult ResendVerification()
    {
        return View();
    }

    // POST: Account/ResendVerification
    [HttpPost]
    public async Task<IActionResult> ResendVerification(string email)
    {
        var user = db.Users.FirstOrDefault(u => u.Email == email);

        if (user == null || user.IsEmailVerified)
        {
            TempData["Info"] = "If an unverified account exists with that email, a verification link has been sent.";
            return RedirectToAction("Login");
        }

        // Create new verification token
        var token = Guid.NewGuid().ToString();
        var verificationToken = new EmailVerificationToken
        {
            Id = Guid.NewGuid().ToString(),
            UserId = user.Id,
            Token = token,
            CreatedAt = DateTime.Now,
            ExpiresAt = DateTime.Now.AddHours(24),
            IsUsed = false
        };

        db.EmailVerificationTokens.Add(verificationToken);
        db.SaveChanges();

        // Generate verification link
        var verificationLink = Url.Action(
            "VerifyEmail",
            "Account",
            new { token = token },
            protocol: Request.Scheme
        );

        // Send verification email
        try
        {
            await emailService.SendVerificationEmailAsync(email, verificationLink!);
            TempData["Info"] = "Verification email has been resent. Please check your inbox.";
        }
        catch (Exception ex)
        {
            // For development: Show link if email fails
            TempData["Info"] = $"Click here to verify: <a href='{verificationLink}'>Verify Email</a>";
        }

        return RedirectToAction("Login");
    }

    // GET: Account/ForgotPassword
    public IActionResult ForgotPassword()
    {
        return View();
    }

    // POST: Account/ForgotPassword
    [HttpPost]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        var user = db.Users.FirstOrDefault(u => u.Email == email);

        if (user != null)
        {
            // Create password reset token
            var token = Guid.NewGuid().ToString();
            var resetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid().ToString(),
                UserId = user.Id,
                Token = token,
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddHours(1),
                IsUsed = false
            };

            db.PasswordResetTokens.Add(resetToken);
            db.SaveChanges();

            // Generate reset link
            var resetLink = Url.Action(
                "ResetPassword",
                "Account",
                new { token = token },
                protocol: Request.Scheme
            );

            // Send reset email
            try
            {
                await emailService.SendPasswordResetEmailAsync(email, resetLink!);
            }
            catch (Exception ex)
            {
                // For development: Show link if email fails
                TempData["Info"] = $"Click here to reset password: <a href='{resetLink}'>Reset Password</a>";
                return RedirectToAction("Login");
            }
        }

        // Always show the same message to prevent email enumeration
        return View("ForgotPasswordConfirmation");
    }

    // GET: Account/ResetPassword?token=xxx
    [HttpGet]
    public IActionResult ResetPassword(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToAction("ForgotPassword");
        }

        var resetToken = db.PasswordResetTokens
            .FirstOrDefault(t => t.Token == token && !t.IsUsed && t.ExpiresAt > DateTime.Now);

        if (resetToken == null)
        {
            TempData["Error"] = "Invalid or expired reset link.";
            return RedirectToAction("ForgotPassword");
        }

        var vm = new ResetPasswordWithTokenVM
        {
            Token = token
        };

        return View(vm);
    }

    // POST: Account/ResetPassword
    [HttpPost]
    public IActionResult ResetPassword(ResetPasswordWithTokenVM vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var resetToken = db.PasswordResetTokens
            .FirstOrDefault(t => t.Token == vm.Token && !t.IsUsed && t.ExpiresAt > DateTime.Now);

        if (resetToken == null)
        {
            TempData["Error"] = "Invalid or expired reset link.";
            return RedirectToAction("ForgotPassword");
        }

        var user = db.Users.Find(resetToken.UserId);
        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("ForgotPassword");
        }

        // Update password
        user.Password = hp.HashPassword(vm.NewPassword);

        // Mark token as used
        resetToken.IsUsed = true;
        resetToken.UsedAt = DateTime.Now;

        db.SaveChanges();

        TempData["Info"] = "Password has been reset successfully. You can now login with your new password.";
        return View("ResetPasswordSuccess");
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

        var u = db.Users.Find(User.Identity!.Name);
        if (u == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("Index", "Home");
        }

        if (!hp.VerifyPassword(u.Password, vm.Current))
        {
            ModelState.AddModelError("Current", "Current password is incorrect.");
            return View(vm);
        }

        if (hp.VerifyPassword(u.Password, vm.New))
        {
            ModelState.AddModelError("New", "New password must be different from current password.");
            return View(vm);
        }

        u.Password = hp.HashPassword(vm.New);
        db.SaveChanges();

        TempData["Info"] = "Password updated successfully.";
        return RedirectToAction();
    }

    // GET: Account/UpdateProfile
    [Authorize(Roles = "Member")]
    public IActionResult UpdateProfile()
    {
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