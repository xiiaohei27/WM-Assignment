using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Main.Models;

#nullable disable warnings

public class LoginVM
{
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    public bool RememberMe { get; set; }
}

public class RegisterVM
{
    [StringLength(100)]
    [EmailAddress]
    [Remote("CheckEmail", "Account", ErrorMessage = "Duplicated {0}.")]
    public string Email { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [Compare("Password")]
    [DataType(DataType.Password)]
    [DisplayName("Confirm Password")]
    public string Confirm { get; set; }

    [StringLength(100)]
    public string Username { get; set; }

    public IFormFile Image { get; set; }

    public string RecaptchaToken { get; set; } = "";
}

public class UpdatePasswordVM
{
    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    [DisplayName("Current Password")]
    public string Current { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    [DisplayName("New Password")]
    public string New { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [Compare("New")]
    [DataType(DataType.Password)]
    [DisplayName("Confirm Password")]
    public string Confirm { get; set; }
}

public class UpdateProfileVM
{
    public string? Email { get; set; }

    [StringLength(100)]
    public string Username { get; set; }

    public string? ImageURL { get; set; }

    public IFormFile? Image { get; set; }
}

// NEW: Password Reset with Token
public class ResetPasswordWithTokenVM
{
    [Required]
    public string Token { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    [DisplayName("New Password")]
    public string NewPassword { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 5)]
    [Compare("NewPassword")]
    [DataType(DataType.Password)]
    [DisplayName("Confirm Password")]
    public string ConfirmPassword { get; set; }
}
public class ResetPasswordVM
{
    [Required]
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }
}

// Add these to your existing Models/ViewModel.cs file

public class EditMemberVM
{
    public string Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Username { get; set; }

    [Required]
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }

    [DisplayName("Email Verified")]
    public bool IsEmailVerified { get; set; }

    public string? ImageURL { get; set; }

    public IFormFile? Image { get; set; }
}

public class FoodCategoryVM
{
    public string? Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; }

    [StringLength(200)]
    public string? Description { get; set; }
}
public class FoodItemVM
{
    public string? Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [Range(0.01, 1000.00)]
    [DisplayFormat(DataFormatString = "{0:F2}")]
    public decimal Price { get; set; }

    [Required]
    [DisplayName("Category")]
    public string CategoryId { get; set; }

    [DisplayName("Available")]
    public bool IsAvailable { get; set; } = true;

    public string? ImageURL { get; set; }

    public IFormFile? Image { get; set; }
}
public class TicketCheckoutVM
{
    public Showtime Showtime { get; set; }
    public List<Seat> Seats { get; set; } = new List<Seat>();
    public List<OrderCartItemVM> FoodCart { get; set; } = new List<OrderCartItemVM>();
    public string RecaptchaToken { get; set; } = "";
}

public class OrderCartItemVM
{
    public string FoodId { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class MovieVM
{
    [MaxLength(30)]
    public string? Id { get; set; }
    [Required]
    [MaxLength(100)]
    public string Title { get; set; }
    [Required]
    public string Genre { get; set; }
    public string? NewGenre { get; set; }
    [Required]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy hh:mm:ss tt}", ApplyFormatInEditMode = true)]
    public DateTime ReleaseDate { get; set; }
    [Required]
    [MaxLength(20)]
    public string Classification { get; set; }
    [Required]
    [MaxLength(50)]
    public string SpokenLanguage { get; set; }
    [Required]
    [Range(1, 500)]
    public int RunningTime { get; set; }
    [Required]
    [MaxLength(100)]
    public string Director { get; set; }
    [Required]
    [MaxLength(200)]
    public string Cast { get; set; }
    [FileExtensions(Extensions = ".mp4")]
    public string? TrailerURL { get; set; }
    [Required]
    public IFormFile? Trailer { get; set; }
    [Required]
    [MaxLength(200)]
    public string Description { get; set; }
    public string? ImageURL { get; set; }
    [Required]
    public IFormFile? Image { get; set; }
}

public class MovieEditVM
{
    [MaxLength(30)]
    public string? Id { get; set; }
    [Required]
    [MaxLength(100)]
    public string Title { get; set; }
    [Required]
    public string Genre { get; set; }
    public string? NewGenre { get; set; }
    [Required]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy hh:mm:ss tt}", ApplyFormatInEditMode = true)]
    public DateTime ReleaseDate { get; set; }
    [Required]
    [MaxLength(20)]
    public string Classification { get; set; }
    [Required]
    [MaxLength(50)]
    public string SpokenLanguage { get; set; }
    [Required]
    [Range(1, 500)]
    public int RunningTime { get; set; }
    [Required]
    [MaxLength(100)]
    public string Director { get; set; }
    [Required]
    [MaxLength(200)]
    public string Cast { get; set; }
    [FileExtensions(Extensions = ".mp4")]
    public string? TrailerURL { get; set; }
    public IFormFile? Trailer { get; set; }
    [Required]
    [MaxLength(200)]
    public string Description { get; set; }
    public string? ImageURL { get; set; }
    public IFormFile? Image { get; set; }
}

public class BookingHistoryVM
{
    public string TicketId { get; set; }
    public string MovieTitle { get; set; }
    public string CinemaName { get; set; }
    public string HallName { get; set; }
    public List<string> SeatNumbers { get; set; } = new List<string>();
    public DateTime Showtime { get; set; }
    public DateTime BookingDate { get; set; }
    public List<OrderCartItemVM> FoodItems { get; set; } = new();
    public string QRCodeData { get; set; } // text to encode in QR
}

public class TicketsSoldVM
{
    public string Movie { get; set; }
    public int TicketsSold { get; set; }
}

public class RevenueVM
{
    public string Movie { get; set; }
    public decimal Revenue { get; set; }
}

public class TicketsOverTimeVM
{
    public string Date { get; set; }
    public int TicketsSold { get; set; }
}

public class RevenueOverTimeVM
{
    public string Date { get; set; }
    public decimal Revenue { get; set; }
}

public class SeatTypeUsageVM
{
    public string SeatType { get; set; }
    public int Count { get; set; }
}

public class ShowtimePerformanceVM
{
    public string Showtime { get; set; }
    public int TicketsSold { get; set; }
}

public class ShowtimeVM 
{
    public string? Id { get; set; }
    [Required]
    public string MovieId { get; set; }
    [Required]
    public string HallId { get; set; }
    [Required]
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TicketPrice { get; set; }
}