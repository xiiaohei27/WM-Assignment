using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Main.Models;

#nullable disable warnings

public class DB(DbContextOptions<DB> options) : DbContext(options)
{
    // DbSet
    public DbSet<User> Users { get; set; }
    public DbSet<Movie> Movies { get; set; }
    public DbSet<Showtime> Showtimes { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<Cinema> Cinemas { get; set; }
    public DbSet<Hall> Halls { get; set; }
    public DbSet<Seat> Seats { get; set; }
    public DbSet<EInvoice> EInvoices { get; set; }
    public DbSet<FoodCategory> FoodCategories { get; set; }
    public DbSet<FoodItem> FoodItems { get; set; }
    public DbSet<FoodOrder> FoodOrders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure TPH inheritance for User/Admin/Member
        modelBuilder.Entity<User>()
            .HasDiscriminator<string>("Discriminator")
            .HasValue<User>("User")
            .HasValue<Admin>("Admin")
            .HasValue<Member>("Member");

        // Make Email unique (but Id is still the primary key)
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
    }
}

// Entity Classes -------------------------------------------------------------

public class User
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required, MaxLength(50)]
    public string Username { get; set; }

    [Required, MaxLength(100)]
    public string Password { get; set; }

    [Required, MaxLength(100)]
    public string Email { get; set; }

    [MaxLength(200)]
    public string Image { get; set; }

    // Map to database column "Role" but make it computed
    [MaxLength(20)]
    [NotMapped]  // Don't store this - it's in the Discriminator column
    public string Role => GetType().Name;
    public List<Ticket> Tickets { get; set; } = new();
    public bool IsEmailVerified { get; set; } = false;

    public DateTime? EmailVerifiedAt { get; set; }

    public List<PasswordResetToken> PasswordResetTokens { get; set; } = new();

    public List<EmailVerificationToken> EmailVerificationTokens { get; set; } = new();
}

public class Admin : User
{
    // Admin has no additional properties
}

public class Member : User
{
    // Member has no additional properties
}

public class PasswordResetToken
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string UserId { get; set; }
    public User User { get; set; }

    [Required, MaxLength(100)]
    public string Token { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; } = false;

    public DateTime? UsedAt { get; set; }
}

public class EmailVerificationToken
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string UserId { get; set; }
    public User User { get; set; }

    [Required, MaxLength(100)]
    public string Token { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; } = false;

    public DateTime? UsedAt { get; set; }
}

public class Movie
{
    [Key]
    public string Id { get; set; }

    [Required, MaxLength(100)]
    public string Title { get; set; }

    [MaxLength(50)]
    public string Genre { get; set; }

    public DateTime ReleaseDate { get; set; }

    [MaxLength(20)]
    public string Classification { get; set; }

    [MaxLength(50)]
    public string SpokenLanguage { get; set; }

    public int RunningTime { get; set; }

    [MaxLength(100)]
    public string Director { get; set; }

    [MaxLength(200)]
    public string Cast { get; set; }

    [MaxLength(200)]
    public string Trailer { get; set; }

    [MaxLength(200)]
    public string Image { get; set; }

    public string Description { get; set; }

    public List<Showtime> Showtimes { get; set; } = new();
}

public class Cinema
{
    [Key]
    public string Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(200)]
    public string StreetAddresses { get; set; }

    [MaxLength(50)]
    public string State { get; set; }

    [MaxLength(50)]
    public string City { get; set; }

    [MaxLength(10)]
    public string PostCode { get; set; }

    [MaxLength(200)]
    public string Image { get; set; }

    public string? Description { get; set; }

    public List<Hall> Halls { get; set; } = new();
}

public class Hall
{
    [Key]
    public string Id { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; }

    [MaxLength(50)]
    public string HallType { get; set; }

    public int Capacity { get; set; }

    public string CinemaId { get; set; }
    public Cinema Cinema { get; set; }

    public List<Seat> Seats { get; set; } = new();
    public List<Showtime> Showtimes { get; set; } = new();
}

public class Seat
{
    [Key]
    public string Id { get; set; }

    [Required, MaxLength(10)]
    public string SeatNumber { get; set; }

    public string HallId { get; set; }
    public Hall Hall { get; set; }

    public List<Ticket> Tickets { get; set; } = new();
}

public class Showtime
{
    [Key]
    public string Id { get; set; }

    public string MovieId { get; set; }
    public string HallId { get; set; }

    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }

    public Movie Movie { get; set; }
    public Hall Hall { get; set; }
    public List<Ticket> Tickets { get; set; } = new();
}

public class Ticket
{
    [Key]
    public string Id { get; set; }

    public string? UserId { get; set; }
    public string? ShowtimeId { get; set; }
    public string? SeatId { get; set; }

    public DateTime BookingDateTime { get; set; } = DateTime.Now;

    public string? EInvoiceId { get; set; }

    public User User { get; set; }
    public Showtime Showtime { get; set; }
    public Seat Seat { get; set; }
    public EInvoice EInvoice { get; set; }
}

public class EInvoice
{
    [Key]
    public string Id { get; set; }

    [Required, MaxLength(50)]
    public string InvoiceNumber { get; set; }

    public DateTime PurchaseDate { get; set; }

    public decimal TotalAmount { get; set; }

    [MaxLength(50)]
    public string PaymentMethod { get; set; }

    [MaxLength(50)]
    public string PaymentStatus { get; set; }

    public List<Ticket> Tickets { get; set; } = new();
}
public class FoodCategory
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required, MaxLength(50)]
    public string Name { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    public List<FoodItem> FoodItems { get; set; } = new();
}

public class FoodItem
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required, MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [MaxLength(200)]
    public string? Image { get; set; }

    public bool IsAvailable { get; set; } = true;

    public string CategoryId { get; set; }
    public FoodCategory Category { get; set; }

    public List<OrderItem> OrderItems { get; set; } = new();
}

public class FoodOrder
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string? UserId { get; set; }
    public User? User { get; set; }

    public DateTime OrderDateTime { get; set; } = DateTime.Now;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Preparing, Ready, Completed, Cancelled, Redeemed

    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    public string? TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    public List<OrderItem> OrderItems { get; set; } = new();

    // QR Code and Redemption fields
    [MaxLength(100)]
    public string? RedemptionCode { get; set; } // Unique code for QR

    public DateTime? ExpiresAt { get; set; } // When the order expires

    public bool IsRedeemed { get; set; } = false;

    public DateTime? RedeemedAt { get; set; }

    [MaxLength(100)]
    public string? RedeemedBy { get; set; } // Staff member who processed redemption
}

public class OrderItem
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string FoodOrderId { get; set; }
    public FoodOrder FoodOrder { get; set; }

    public string FoodItemId { get; set; }
    public FoodItem FoodItem { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }
}
