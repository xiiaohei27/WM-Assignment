using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Net.Mail;
using System.Security.Claims;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace Main;

public class Helper(IWebHostEnvironment en,
                    IHttpContextAccessor ct,
                    IConfiguration cf)
{
    // ------------------------------------------------------------------------
    // Photo Upload
    // ------------------------------------------------------------------------

    public string ValidatePhoto(IFormFile f)
    {
        var reType = new Regex(@"^image\/(jpeg|png)$", RegexOptions.IgnoreCase);
        var reName = new Regex(@"^.+\.(jpeg|jpg|png)$", RegexOptions.IgnoreCase);

        if (!reType.IsMatch(f.ContentType) || !reName.IsMatch(f.FileName))
        {
            return "Only JPG and PNG photo is allowed.";
        }
        else if (f.Length > 5 * 1024 * 1024)
        {
            return "Photo size cannot more than 5MB.";
        }

        return "";
    }

    public string SavePhoto(IFormFile f, string folder)
    {
        var file = Guid.NewGuid().ToString("n") + ".jpg";
        var path = Path.Combine(en.WebRootPath, folder, file);

        var options = new ResizeOptions
        {
            Size = new(200, 200),
            Mode = ResizeMode.Crop,
        };

        using var stream = f.OpenReadStream();
        using var img = SixLabors.ImageSharp.Image.Load(stream); // Remove the configuration parameter
        img.Mutate(x => x.Resize(options));
        img.SaveAsJpeg(path); // Use SaveAsJpeg instead of Save

        return file;
    }

    public void DeletePhoto(string file, string folder)
    {
        file = Path.GetFileName(file);
        var path = Path.Combine(en.WebRootPath, folder, file);
        File.Delete(path);
    }



    // ------------------------------------------------------------------------
    // Security Helper Functions
    // ------------------------------------------------------------------------


    private readonly PasswordHasher<object> ph = new();

    public string HashPassword(string password)
    {
        return ph.HashPassword(0, password);
    }

    public bool VerifyPassword(string hash, string password)
    {
        return ph.VerifyHashedPassword(0, hash, password)
               == PasswordVerificationResult.Success;
    }

    public void SignIn(string email, string role, bool rememberMe)
    {
        // (1) Claim, identity and principal
        List<Claim> claims =
            [
                new(ClaimTypes.Name, email),
                new(ClaimTypes.Role, role),
            ];

        ClaimsIdentity identity = new(claims, "Cookies");

        ClaimsPrincipal principal = new(identity);

        // (2) Remember me (authentication properties)
        AuthenticationProperties properties = new()
        {
            IsPersistent = rememberMe,
        };

        // (3) Sign in
        ct.HttpContext!.SignInAsync(principal, properties);
    }

    public void SignOut()
    {
        // Sign out
        ct.HttpContext?.SignOutAsync();
    }

    public string RandomPassword()
    {
        string s = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string password = "";

        Random r = new();

        for (int i = 1; i <= 10; i++)
        {
            password += s[r.Next(s.Length)];
        }

        return password;
    }

    //// ------------------------------------------------------------------------
    //// Email
    //// ------------------------------------------------------------------------

    //public void SendEmail(MailMessage mail)
    //{
    //    string user = cf["Smtp:User"]!;
    //    string pass = cf["Smtp:Pass"]!;
    //    string host = cf["Smtp:Host"]!;
    //    string name = cf["Smtp:Name"]!;
    //    int port = cf.GetValue<int>(cf["Smtp:Port"]!);

    //    mail.From = new MailAddress(user, name);

    //    using var smtp = new SmtpClient
    //    {
    //        Host = host,
    //        Port = port,
    //        EnableSsl = true,
    //        Credentials = new System.Net.NetworkCredential(user, pass),
    //    };

    //    smtp.Send(mail);

    //}

    // ------------------------------------------------------------------------
    // Video Upload
    // ------------------------------------------------------------------------

    public string SaveVideo(IFormFile f, string folder)
    {
        var file = Guid.NewGuid().ToString("n") + ".mp4"; // preserve extension
        var path = Path.Combine(en.WebRootPath, folder, file);

        using var stream = new FileStream(path, FileMode.Create);
        f.CopyTo(stream);

        return file;
    }

    public string ValidateVideo(IFormFile f)
    {
        var reType = new Regex(@"^video/mp4$", RegexOptions.IgnoreCase);
        var reName = new Regex(@"^.+\.mp4$", RegexOptions.IgnoreCase);

        if (!reType.IsMatch(f.ContentType) || !reName.IsMatch(f.FileName))
        {
            return "Only MP4 video is allowed.";
        }
        else if (f.Length > 100 * 1024 * 1024)
        {
            return "Video size cannot exceed 100 MB.";
        }

        return "";
    }
}
