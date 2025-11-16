using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Net.Sockets;

namespace Main.Models;

#nullable disable warnings

public class DB(DbContextOptions options) : DbContext(options)
{
    // DbSet
    public DbSet<User> Users { get; set; }
    public DbSet<Movie> Movies { get; set; }
    public DbSet<Showtime> Showtimes { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
}

// Entity Classes -------------------------------------------------------------

public class User
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Username { get; set; }

    [Required, MaxLength(50)]
    public string Password { get; set; }

    [Required, MaxLength(10)]
    public string Role { get; set; }   // Admin / Customer

    public List<Ticket> Tickets { get; set; } = new();
}

public class Movie
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Title { get; set; }

    [MaxLength(50)]
    public string Genre { get; set; }

    public DateTime ReleaseDate { get; set; }

    [MaxLength(200)]
    public string Image { get; set; }

    public List<Showtime> Showtimes { get; set; } = new();
}

public class Showtime
{
    [Key]
    public int Id { get; set; }

    public int MovieId { get; set; }   // FK

    public DateTime StartTime { get; set; }

    public int TotalSeats { get; set; }

    public Movie Movie { get; set; }
    public List<Ticket> Tickets { get; set; } = new();
}

public class Ticket
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }      // FK
    public int ShowtimeId { get; set; }  // FK

    public int SeatNumber { get; set; }

    public DateTime BookingTime { get; set; } = DateTime.Now;

    public User User { get; set; }
    public Showtime Showtime { get; set; }
}