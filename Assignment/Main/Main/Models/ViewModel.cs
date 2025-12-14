using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

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
}

public class OrderCartItemVM
{
    public string FoodId { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}