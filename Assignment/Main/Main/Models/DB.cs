using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Net.Sockets;

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
}

// Entity Classes -------------------------------------------------------------

public class User
{
    [Key]
    public string Id { get; set; }

    [Required, MaxLength(50)]
    public string Username { get; set; }

    [Required, MaxLength(100)]
    public string Password { get; set; }

    [Required, MaxLength(100)]
    public string Email { get; set; }

    [MaxLength(200)]
    public string Image { get; set; }

    [Required, MaxLength(20)]
    public string Role { get; set; }   // Admin || Customer

    public List<Ticket> Tickets { get; set; } = new();
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

    public int RunningTime { get; set; }   // in minutes?

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
    public string HallType { get; set; }   // IMAX, 2D, 3D, etc.

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
