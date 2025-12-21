using Main.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace Main.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(DB db, Helper hp) : Controller
{
    // ============================================================================
    // MEMBER MANAGEMENT
    // ============================================================================

    // GET: Admin/Members
    public IActionResult Members(string? search)
    {
        var members = db.Users.OfType<Member>().AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            members = members.Where(m =>
                m.Username.Contains(search) ||
                m.Email.Contains(search));
        }

        ViewBag.Search = search;
        return View(members.OrderBy(m => m.Username).ToList());
    }

    // GET: Admin/MemberDetails/5
    public IActionResult MemberDetails(string id)
    {
        var member = db.Users.OfType<Member>()
            .Include(m => m.Tickets)
                .ThenInclude(t => t.Showtime)
                .ThenInclude(s => s.Movie)
            .FirstOrDefault(m => m.Id == id);

        if (member == null)
        {
            TempData["Error"] = "Member not found.";
            return RedirectToAction(nameof(Members));
        }

        return View(member);
    }

    // GET: Admin/EditMember/5
    public IActionResult EditMember(string id)
    {
        var member = db.Users.OfType<Member>().FirstOrDefault(m => m.Id == id);
        if (member == null)
        {
            TempData["Error"] = "Member not found.";
            return RedirectToAction(nameof(Members));
        }

        var vm = new EditMemberVM
        {
            Id = member.Id,
            Username = member.Username,
            Email = member.Email,
            IsEmailVerified = member.IsEmailVerified,
            ImageURL = member.Image
        };

        return View(vm);
    }

    // POST: Admin/EditMember
    [HttpPost]
    public IActionResult EditMember(EditMemberVM vm)
    {
        var member = db.Users.OfType<Member>().FirstOrDefault(m => m.Id == vm.Id);
        if (member == null)
        {
            TempData["Error"] = "Member not found.";
            return RedirectToAction(nameof(Members));
        }

        // Check email uniqueness (excluding current member)
        if (db.Users.Any(u => u.Email == vm.Email && u.Id != vm.Id))
        {
            ModelState.AddModelError("Email", "Email already exists.");
        }

        if (vm.Image != null)
        {
            var err = hp.ValidatePhoto(vm.Image);
            if (err != "") ModelState.AddModelError("Image", err);
        }

        if (ModelState.IsValid)
        {
            member.Username = vm.Username;
            member.Email = vm.Email;
            member.IsEmailVerified = vm.IsEmailVerified;

            if (vm.Image != null)
            {
                hp.DeletePhoto(member.Image, "photos");
                member.Image = hp.SavePhoto(vm.Image, "photos");
            }

            db.SaveChanges();
            TempData["Info"] = "Member updated successfully.";
            return RedirectToAction(nameof(Members));
        }

        vm.ImageURL = member.Image;
        return View(vm);
    }

    // GET: Admin/DeleteMember/5
    public IActionResult DeleteMember(string id)
    {
        var member = db.Users.OfType<Member>().FirstOrDefault(m => m.Id == id);
        if (member == null)
        {
            TempData["Error"] = "Member not found.";
            return RedirectToAction(nameof(Members));
        }

        return View(member);
    }

    // POST: Admin/DeleteMember/5
    [HttpPost, ActionName("DeleteMember")]
    public IActionResult DeleteMemberConfirmed(string id)
    {
        var member = db.Users.OfType<Member>().FirstOrDefault(m => m.Id == id);
        if (member == null)
        {
            TempData["Error"] = "Member not found.";
            return RedirectToAction(nameof(Members));
        }

        // Delete member's photo
        hp.DeletePhoto(member.Image, "photos");

        // Delete member
        db.Users.Remove(member);
        db.SaveChanges();

        TempData["Info"] = "Member deleted successfully.";
        return RedirectToAction(nameof(Members));
    }

    [HttpGet]
    public JsonResult GetMembersByFilter(string? search)
    {
        var members = db.Users.OfType<Member>().AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            members = members.Where(m =>
                m.Username.Contains(search) ||
                m.Email.Contains(search));
        }

        var result = members
            .OrderBy(m => m.Username)
            .Select(m => new
            {
                id = m.Id,
                username = m.Username,
                email = m.Email,
                image = m.Image,
                isEmailVerified = m.IsEmailVerified
            })
            .ToList();

        return Json(result);
    }

    // ============================================================================
    // FOOD CATEGORY MANAGEMENT
    // ============================================================================

    // GET: Admin/FoodCategories
    public IActionResult FoodCategories()
    {
        var categories = db.FoodCategories
            .Include(c => c.FoodItems)
            .OrderBy(c => c.Name)
            .ToList();
        return View(categories);
    }

    // GET: Admin/CreateCategory
    public IActionResult CreateCategory()
    {
        return View();
    }

    // POST: Admin/CreateCategory
    [HttpPost]
    public IActionResult CreateCategory(FoodCategoryVM vm)
    {
        if (ModelState.IsValid)
        {
            var category = new FoodCategory
            {
                Id = Guid.NewGuid().ToString(),
                Name = vm.Name,
                Description = vm.Description
            };

            db.FoodCategories.Add(category);
            db.SaveChanges();

            TempData["Info"] = "Category created successfully.";
            return RedirectToAction(nameof(FoodCategories));
        }

        return View(vm);
    }

    // GET: Admin/EditCategory/5
    public IActionResult EditCategory(string id)
    {
        var category = db.FoodCategories.Find(id);
        if (category == null)
        {
            TempData["Error"] = "Category not found.";
            return RedirectToAction(nameof(FoodCategories));
        }

        var vm = new FoodCategoryVM
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        };

        return View(vm);
    }


    // POST: Admin/EditCategory
    [HttpPost]
    public IActionResult EditCategory(FoodCategoryVM vm)
    {
        if (ModelState.IsValid)
        {
            var category = db.FoodCategories.Find(vm.Id);
            if (category == null)
            {
                TempData["Error"] = "Category not found.";
                return RedirectToAction(nameof(FoodCategories));
            }

            category.Name = vm.Name;
            category.Description = vm.Description;
            db.SaveChanges();

            TempData["Info"] = "Category updated successfully.";
            return RedirectToAction(nameof(FoodCategories));
        }

        return View(vm);
    }

    // GET: Admin/DeleteCategory/5
    public IActionResult DeleteCategory(string id)
    {
        var category = db.FoodCategories
            .Include(c => c.FoodItems)
            .FirstOrDefault(c => c.Id == id);

        if (category == null)
        {
            TempData["Error"] = "Category not found.";
            return RedirectToAction(nameof(FoodCategories));
        }

        return View(category);
    }

    // POST: Admin/DeleteCategory/5
    [HttpPost, ActionName("DeleteCategory")]
    public IActionResult DeleteCategoryConfirmed(string id)
    {
        var category = db.FoodCategories
            .Include(c => c.FoodItems)
            .FirstOrDefault(c => c.Id == id);

        if (category == null)
        {
            TempData["Error"] = "Category not found.";
            return RedirectToAction(nameof(FoodCategories));
        }

        if (category.FoodItems.Any())
        {
            TempData["Error"] = "Cannot delete category with existing food items.";
            return RedirectToAction(nameof(FoodCategories));
        }

        db.FoodCategories.Remove(category);
        db.SaveChanges();

        TempData["Info"] = "Category deleted successfully.";
        return RedirectToAction(nameof(FoodCategories));
    }

    // ============================================================================
    // FOOD ITEM MANAGEMENT
    // ============================================================================

    // GET: Admin/FoodItems
    public IActionResult FoodItems(string? categoryId, string? search)
    {
        var items = db.FoodItems.Include(f => f.Category).AsQueryable();

        if (!string.IsNullOrEmpty(categoryId))
        {
            items = items.Where(f => f.CategoryId == categoryId);
        }

        if (!string.IsNullOrEmpty(search))
        {
            items = items.Where(f => f.Name.Contains(search));
        }

        ViewBag.Categories = db.FoodCategories.OrderBy(c => c.Name).ToList();
        ViewBag.CategoryId = categoryId;
        ViewBag.Search = search;

        return View(items.OrderBy(f => f.Name).ToList());
    }

    // GET: Admin/CreateFoodItem
    public IActionResult CreateFoodItem()
    {
        ViewBag.Categories = db.FoodCategories.OrderBy(c => c.Name).ToList();
        return View();
    }

    // POST: Admin/CreateFoodItem
    [HttpPost]
    public IActionResult CreateFoodItem(FoodItemVM vm)
    {
        if (vm.Image != null)
        {
            var err = hp.ValidatePhoto(vm.Image);
            if (err != "") ModelState.AddModelError("Image", err);
        }

        if (ModelState.IsValid)
        {
            var item = new FoodItem
            {
                Id = Guid.NewGuid().ToString(),
                Name = vm.Name,
                Description = vm.Description,
                Price = vm.Price,
                CategoryId = vm.CategoryId,
                IsAvailable = vm.IsAvailable,
                Image = vm.Image != null ? hp.SavePhoto(vm.Image, "food") : null
            };

            db.FoodItems.Add(item);
            db.SaveChanges();

            TempData["Info"] = "Food item created successfully.";
            return RedirectToAction(nameof(FoodItems));
        }

        ViewBag.Categories = db.FoodCategories.OrderBy(c => c.Name).ToList();
        return View(vm);
    }

    // GET: Admin/EditFoodItem/
    public IActionResult EditFoodItem(string id)
    {
        var item = db.FoodItems.Find(id);
        if (item == null)
        {
            TempData["Error"] = "Food item not found.";
            return RedirectToAction(nameof(FoodItems));
        }

        var vm = new FoodItemVM
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            CategoryId = item.CategoryId,
            IsAvailable = item.IsAvailable,
            ImageURL = item.Image
        };

        ViewBag.Categories = db.FoodCategories.OrderBy(c => c.Name).ToList();
        return View(vm);
    }

    // POST: Admin/EditFoodItem
    [HttpPost]
    public IActionResult EditFoodItem(FoodItemVM vm)
    {
        var item = db.FoodItems.Find(vm.Id);
        if (item == null)
        {
            TempData["Error"] = "Food item not found.";
            return RedirectToAction(nameof(FoodItems));
        }

        if (vm.Image != null)
        {
            var err = hp.ValidatePhoto(vm.Image);
            if (err != "") ModelState.AddModelError("Image", err);
        }

        if (ModelState.IsValid)
        {
            item.Name = vm.Name;
            item.Description = vm.Description;
            item.Price = vm.Price;
            item.CategoryId = vm.CategoryId;
            item.IsAvailable = vm.IsAvailable;

            if (vm.Image != null)
            {
                if (!string.IsNullOrEmpty(item.Image))
                {
                    hp.DeletePhoto(item.Image, "food");
                }
                item.Image = hp.SavePhoto(vm.Image, "food");
            }

            db.SaveChanges();
            TempData["Info"] = "Food item updated successfully.";
            return RedirectToAction(nameof(FoodItems));
        }

        vm.ImageURL = item.Image;
        ViewBag.Categories = db.FoodCategories.OrderBy(c => c.Name).ToList();
        return View(vm);
    }

    // GET: Admin/DeleteFoodItem/5
    public IActionResult DeleteFoodItem(string id)
    {
        var item = db.FoodItems.Include(f => f.Category).FirstOrDefault(f => f.Id == id);
        if (item == null)
        {
            TempData["Error"] = "Food item not found.";
            return RedirectToAction(nameof(FoodItems));
        }

        return View(item);
    }

    // POST: Admin/DeleteFoodItem/5
    [HttpPost, ActionName("DeleteFoodItem")]
    public IActionResult DeleteFoodItemConfirmed(string id)
    {
        var item = db.FoodItems.Find(id);
        if (item == null)
        {
            TempData["Error"] = "Food item not found.";
            return RedirectToAction(nameof(FoodItems));
        }

        if (!string.IsNullOrEmpty(item.Image))
        {
            hp.DeletePhoto(item.Image, "food");
        }

        db.FoodItems.Remove(item);
        db.SaveChanges();

        TempData["Info"] = "Food item deleted successfully.";
        return RedirectToAction(nameof(FoodItems));
    }

    [HttpGet]
    public IActionResult GetFoodItemsByFilter(string? categoryId, string? search)
    {
        var query = db.FoodItems
            .Include(f => f.Category)
            .AsQueryable();

        // Filter by category
        if (!string.IsNullOrEmpty(categoryId))
        {
            query = query.Where(f => f.CategoryId == categoryId);
        }

        // Search by name
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(f => f.Name.Contains(search));
        }

        var items = query
            .OrderBy(f => f.Name)
            .Select(f => new
            {
                id = f.Id,
                name = f.Name,
                description = f.Description,
                price = f.Price,
                image = f.Image,
                isAvailable = f.IsAvailable,
                categoryName = f.Category.Name,
                categoryId = f.CategoryId
            })
            .ToList();

        return Json(items);
    }

    // GET: Admin/Dashboard
    public IActionResult Dashboard()
    {
        ViewBag.TotalMembers = db.Users.OfType<Member>().Count();
        ViewBag.TotalFoodItems = db.FoodItems.Count();
        ViewBag.TotalCategories = db.FoodCategories.Count();
        ViewBag.TotalOrders = db.FoodOrders.Count();
        ViewBag.TodayOrders = db.FoodOrders.Count(o => o.OrderDateTime.Date == DateTime.Today);

        return View();
    }

    // ============================================================================
    // MOVIE MANAGEMENT
    // ============================================================================

    //GET: Admin/MovieManage
    public IActionResult MovieManage(string? genre, string? search)
    {
        var movie = db.Movies.AsQueryable();

        if (!string.IsNullOrEmpty(genre))
        {
            movie = movie.Where(m => m.Genre == genre);
        }

        if (!string.IsNullOrEmpty(search))
        {
            movie = movie.Where(m => m.Title.Contains(search));
        }

        ViewBag.Genre = db.Movies.Select(m => m.Genre).Distinct().OrderBy(g => g).ToList();
        ViewBag.SelectedGenre = genre;
        ViewBag.Search = search;

        return View(movie.OrderBy(m => m.Genre).ToList());
    }

    // GET: Admin/GetMoviesByFilter
    [HttpGet]
    public JsonResult GetMoviesByFilter(string? genre, string? search)
    {
        var movies = db.Movies.AsQueryable();

        if (!string.IsNullOrEmpty(genre))
            movies = movies.Where(m => m.Genre == genre);

        if (!string.IsNullOrEmpty(search))
            movies = movies.Where(m => m.Title.Contains(search));

        var result = movies
            .OrderBy(m => m.Genre)
            .Select(m => new
            {
                Id = m.Id,
                Title = m.Title,
                Genre = m.Genre,
                ReleaseDate = m.ReleaseDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                Classification = m.Classification,
                SpokenLanguage = m.SpokenLanguage,
                RunningTime = m.RunningTime.ToString() ?? "", // if nullable
                Director = m.Director,
                Cast = m.Cast,
                Image = m.Image,
                Description = m.Description
            })
            .ToList();

        return Json(result);
    }

    //GET: Admin/CreateMovie
    public IActionResult CreateMovie()
    {
        ViewBag.Genre = db.Movies.Select(m => m.Genre).Distinct().OrderBy(g => g).ToList();
        return View(new MovieVM());
    }

    //POST: Admin/CreateMovie
    [HttpPost]
    public IActionResult CreateMovie(MovieVM vm)
    {
        if (vm.Trailer != null)
        {
            var e = hp.ValidateVideo(vm.Trailer);
            if (e != "") ModelState.AddModelError("Trailer", e);
        }

        if (vm.Image != null)
        {
            var e = hp.ValidatePhoto(vm.Image);
            if (e != "") ModelState.AddModelError("Image", e);
        }

        if (db.Movies.Any(m => m.Title == vm.Title))
        {
            ModelState.AddModelError("Title", "Duplicate Title.");
        }

        if (vm.Genre == "new")
        {
            if (string.IsNullOrWhiteSpace(vm.NewGenre))
            {
                ModelState.AddModelError("Genre", "Please enter a genre.");
            }
            else
            {
                vm.Genre = vm.NewGenre.Trim();
            }
        }

        if (ModelState.IsValid)
        {
            string newId;
            var lastMovie = db.Movies.OrderByDescending(m => m.Id).FirstOrDefault();
            if (lastMovie == null)
            {
                newId = "M001"; // first movie
            }
            else
            {
                int lastNumber = int.Parse(lastMovie.Id.Substring(1));
                newId = $"M{(lastNumber + 1):D3}";
            }

            var movie = new Movie
            {
                Id = newId,
                Title = vm.Title,
                Genre = vm.Genre,
                ReleaseDate = vm.ReleaseDate,
                Classification = vm.Classification,
                SpokenLanguage = vm.SpokenLanguage,
                RunningTime = vm.RunningTime,
                Director = vm.Director,
                Cast = vm.Cast,
                Description = vm.Description,
                Trailer = vm.Trailer != null ? hp.SaveVideo(vm.Trailer, "trailers") : null,
                Image = vm.Image != null ? hp.SavePhoto(vm.Image, "images") : null
            };

            db.Movies.Add(movie);
            db.SaveChanges();

            TempData["Info"] = "Movie created successfully.";
            return RedirectToAction(nameof(MovieManage));
        }
        ViewBag.Genre = db.Movies.Select(m => m.Genre).Distinct().OrderBy(g => g).ToList();
        return View(vm);
    }

    // GET: Admin/EditMovie
    public IActionResult EditMovie(string id)
    {
        var m = db.Movies.Find(id);
        if (m == null)
        {
            TempData["Error"] = "Movie not found.";
            return RedirectToAction(nameof(MovieManage));
        }

        var vm = new MovieEditVM
        {
            Id = m.Id,
            Title = m.Title,
            Genre = m.Genre,
            ReleaseDate = m.ReleaseDate,
            Classification = m.Classification,
            SpokenLanguage = m.SpokenLanguage,
            RunningTime = m.RunningTime,
            Director = m.Director,
            Cast = m.Cast,
            Description = m.Description,
            ImageURL = m.Image,
            TrailerURL = m.Trailer
        };

        ViewBag.Genre = db.Movies.Select(m => m.Genre).Distinct().OrderBy(g => g).ToList();
        return View(vm);
    }

    //POST: Admin/EditMovie
    [HttpPost]
    public IActionResult EditMovie(MovieEditVM vm)
    {
        var m = db.Movies.Find(vm.Id);

        if (m == null)
        {
            TempData["Error"] = "Movie not found.";
            return RedirectToAction(nameof(MovieManage));
        }

        if (vm.Trailer != null)
        {
            var e = hp.ValidateVideo(vm.Trailer);
            if (e != "") ModelState.AddModelError("Trailer", e);
        }

        if (vm.Image != null)
        {
            var e = hp.ValidatePhoto(vm.Image);
            if (e != "") ModelState.AddModelError("Image", e);
        }

        if (db.Movies.Any(m => m.Title == vm.Title && m.Id != vm.Id))
        {
            ModelState.AddModelError("Title", "Duplicate Title.");
        }

        if (vm.Genre == "new")
        {
            if (string.IsNullOrWhiteSpace(vm.NewGenre))
            {
                ModelState.AddModelError("Genre", "Please enter a genre.");
            }
            else
            {
                vm.Genre = vm.NewGenre.Trim();
            }
        }

        if (ModelState.IsValid)
        {
            m.Title = vm.Title;
            m.Genre = vm.Genre;
            m.ReleaseDate = vm.ReleaseDate;
            m.Classification = vm.Classification;
            m.SpokenLanguage = vm.SpokenLanguage;
            m.RunningTime = vm.RunningTime;
            m.Director = vm.Director;
            m.Cast = vm.Cast;
            m.Description = vm.Description;

            if (vm.Image != null)
            {
                if (!string.IsNullOrEmpty(m.Image))
                {
                    hp.DeletePhoto(m.Image, "images");
                }
                m.Image = hp.SavePhoto(vm.Image, "images");
            }

            if (vm.Trailer != null)
            {
                if (!string.IsNullOrEmpty(m.Trailer))
                {
                    hp.DeletePhoto(m.Trailer, "trailers");
                }
                m.Trailer = hp.SaveVideo(vm.Trailer, "trailers");
            }
            db.SaveChanges();

            TempData["Info"] = "Movie Details updated successfully";
            return RedirectToAction(nameof(MovieManage));
        }
        ViewBag.Genre = db.Movies.Select(m => m.Genre).Distinct().OrderBy(g => g).ToList();
        return View(vm);
    }

    //GET: Admin/DeleteMovie
    public IActionResult DeleteMovie(string id)
    {
        var movie = db.Movies.Find(id);
        if (movie == null)
        {
            TempData["Error"] = "Movie not found.";
            return RedirectToAction(nameof(MovieManage));
        }

        return View(movie);
    }

    // POST: Admin/DeleteMovie/
    [HttpPost, ActionName("DeleteMovie")]
    [ValidateAntiForgeryToken] //prevent CSRF attack (for security)
    public IActionResult DeleteMovieConfirmation(string id)
    {
        var m = db.Movies.Find(id);
        if (m == null)
        {
            TempData["Error"] = "Movie not found.";
            return RedirectToAction(nameof(MovieManage));
        }

        if (!string.IsNullOrEmpty(m.Image))
        {
            hp.DeletePhoto(m.Image, "images");
        }

        if (!string.IsNullOrEmpty(m.Trailer))
        {
            hp.DeletePhoto(m.Trailer, "trailers");
        }

        db.Movies.Remove(m);
        db.SaveChanges();

        TempData["Info"] = "Movie deleted successfully.";
        return RedirectToAction(nameof(MovieManage));
    }

    public IActionResult Report()
    {
        return View(); // Returns Report.cshtml
    }

    // Tickets sold per movie
    public IActionResult TicketsSoldReport()
    {
        var movieSales = db.Tickets
            .Include(t => t.Showtime)
                .ThenInclude(s => s.Movie)
            .GroupBy(t => t.Showtime.Movie.Title)
            .Select(g => new TicketsSoldVM
            {
                Movie = g.Key,
                TicketsSold = g.Count()
            })
            .ToList();

        return Json(movieSales);
    }

    // Revenue earned per movie
    public IActionResult RevenueReport()
    {
        var revenue = db.Tickets
            .Include(t => t.Showtime).ThenInclude(s => s.Movie)
            .Include(t => t.TicketSeats).ThenInclude(ts => ts.Seat)
            .AsEnumerable()
            .GroupBy(t => t.Showtime.Movie.Title)
            .Select(g => new RevenueVM
            {
                Movie = g.Key,
                Revenue = g.Sum(t => t.TicketSeats.Sum(ts => t.Showtime.TicketPrice * ts.Seat.Multiplier))
            })
            .ToList();

        return Json(revenue);
    }

    // Tickets sold over time
    public IActionResult TicketsOverTimeReport()
    {
        var data = db.Tickets
            .GroupBy(t => t.BookingDateTime.Date)
            .Select(g => new TicketsOverTimeVM
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                TicketsSold = g.Count()
            })
            .ToList();

        return Json(data);
    }

    // Revenue over time
    public IActionResult RevenueOverTimeReport()
    {
        var data = db.Tickets
            .Include(t => t.TicketSeats)
                .ThenInclude(ts => ts.Seat)
            .Include(t => t.Showtime) 
            .AsEnumerable()
            .GroupBy(t => t.BookingDateTime.Date)
            .Select(g => new RevenueOverTimeVM
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                Revenue = g.Sum(t =>
                    t.TicketSeats.Sum(ts => t.Showtime.TicketPrice * ts.Seat.Multiplier))
            })
            .ToList();

        return Json(data);
    }

    // Seat type usage
    public IActionResult SeatTypeUsageReport()
    {
        var data = db.TicketSeats
            .Include(ts => ts.Seat)
            .GroupBy(ts => ts.Seat.SeatType)
            .Select(g => new SeatTypeUsageVM
            {
                SeatType = g.Key,
                Count = g.Count()
            })
            .ToList();

        return Json(data);
    }

    // Showtime performance
    public IActionResult ShowtimePerformanceReport()
    {
        var data = db.Tickets
            .Include(t => t.Showtime)
            .GroupBy(t => t.Showtime.StartDateTime)
            .Select(g => new ShowtimePerformanceVM
            {
                Showtime = g.Key.ToString("yyyy-MM-dd HH:mm"),
                TicketsSold = g.Count()
            })
            .ToList();

        return Json(data);
    }

    // ============================================================================
    // SHOWTIME MANAGEMENT
    // ============================================================================

    // GET: Admin/ShowtimeManage
    public IActionResult ShowtimeManage(string? movieId, string? search)
    {
        var showtimes = db.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Hall)
            .AsQueryable();
        if (!string.IsNullOrEmpty(movieId))
        {
            showtimes = showtimes.Where(s => s.MovieId == movieId);
        }
        if (!string.IsNullOrEmpty(search))
        {
            showtimes = showtimes.Where(s =>
                s.Movie.Title.Contains(search));
        }
        ViewBag.Movies = db.Movies.OrderBy(m => m.Title).ToList();
        ViewBag.SelectedMovieId = movieId;
        ViewBag.Search = search;
        return View(showtimes.OrderBy(s => s.StartDateTime).ToList());
    }

    // GET: Admin/GetShowtimeByFilter
    [HttpGet]
    public JsonResult GetShowtimeByFilter(string? movieId, string? search)
    {
        var showtime = db.Showtimes.Include(s => s.Movie).Include(s => s.Hall).AsQueryable();

        if (!string.IsNullOrEmpty(movieId))
            showtime = showtime.Where(s => s.MovieId == movieId);

        if (!string.IsNullOrEmpty(search))
            showtime = showtime.Where(s => s.Movie.Title.Contains(search));

        var result = showtime
            .OrderBy(s => s.StartDateTime)
            .Select(s => new
            {
                Id = s.Id,
                MovieId = s.MovieId,
                HallId = s.HallId,
                MovieTitle = s.Movie.Title,
                Image = s.Movie.Image,
                StartDateTime = s.StartDateTime,
                EndDateTime = s.EndDateTime,
                TicketPrice = s.TicketPrice
            })
            .ToList();

        return Json(result);
    }

    // GET: Admin/CreateShowtime
    public IActionResult CreateShowtime()
    {
        ViewBag.Movies = db.Movies.ToList();
        ViewBag.Halls = db.Halls.OrderBy(h => h.Id).ToList();
        ViewBag.Cinemas = db.Cinemas.OrderBy(c => c.Id).ToList();
        return View(new ShowtimeVM());
    }

    // POST: Admin/CreateShowtime
    [HttpPost]
    public IActionResult CreateShowtime(ShowtimeVM vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Errors"] = string.Join(" | ",
                ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

            ViewBag.Movies = db.Movies.ToList();
            ViewBag.Halls = db.Halls.ToList();
            ViewBag.Cinemas = db.Cinemas.ToList();
            return View(vm);
        }

        if (ModelState.IsValid)
        {
            string newId;
            var lastShowtime = db.Showtimes.OrderByDescending(s => s.Id).FirstOrDefault();
            if (lastShowtime == null)
            {
                newId = "ST001";
            }
            else
            {
                int lastNumber = int.Parse(lastShowtime.Id.Substring(2));
                newId = $"ST{(lastNumber + 1):D3}";
            }
            //Calculate EndDateTime based on Movie RunningTime
            var movie = db.Movies.FirstOrDefault(m => m.Id == vm.MovieId);
            if (movie == null)
            {
                ModelState.AddModelError("MovieId", "Selected movie not found.");
                ViewBag.Movies = db.Movies.ToList();
                ViewBag.Halls = db.Halls.OrderBy(h => h.Id).ToList();
                ViewBag.Cinemas = db.Cinemas.OrderBy(c => c.Id).ToList();
                return View(vm);
            }

            vm.EndDateTime = vm.StartDateTime.AddMinutes(movie.RunningTime);

            var showtime = new Showtime
            {
                Id = newId,
                MovieId = vm.MovieId,
                HallId = vm.HallId,
                StartDateTime = vm.StartDateTime,
                EndDateTime = vm.EndDateTime,
                TicketPrice = vm.TicketPrice
            };

            db.Showtimes.Add(showtime);
            db.SaveChanges();

            TempData["Info"] = "Showtime created successfully.";
            return RedirectToAction(nameof(ShowtimeManage));
        }

        ViewBag.Movies = db.Movies.ToList();
        ViewBag.Halls = db.Halls.OrderBy(h => h.Id).ToList();
        ViewBag.Cinemas = db.Cinemas.OrderBy(c => c.Id).ToList();
        return View(vm);
    }

    // GET: Admin/DeleteShowtime
    public IActionResult DeleteShowtime(string id)
    {
        var showtime = db.Showtimes.Include(s => s.Movie).Include(s => s.Hall).FirstOrDefault(s => s.Id == id);
        if (showtime == null)
        {
            TempData["Error"] = "Movie Showtime not found.";
            return RedirectToAction(nameof(ShowtimeManage));
        }

        return View(showtime);
    }

    // POST: Admin/DeleteShowtime/
    [HttpPost, ActionName("DeleteShowtime")]
    [ValidateAntiForgeryToken] //prevent CSRF attack (for security)
    public IActionResult DeleteShowtimeConfirmation(string id)
    {
        var s = db.Showtimes.Find(id);
        if (s == null)
        {
            TempData["Error"] = "Movie Showtime not found.";
            return RedirectToAction(nameof(ShowtimeManage));
        }


        db.Showtimes.Remove(s);
        db.SaveChanges();

        TempData["Info"] = "Movie Showtime deleted successfully.";
        return RedirectToAction(nameof(ShowtimeManage));
    }

    // GET: Admin/EditShowtime
    public IActionResult EditShowtime(string id)
    {
        var s = db.Showtimes.Find(id);
        if (s == null)
        {
            TempData["Error"] = "Movie Showtime not found.";
            return RedirectToAction(nameof(ShowtimeManage));
        }

        var vm = new ShowtimeVM
        {
            Id = s.Id,
            MovieId = s.MovieId,
            HallId = s.HallId,
            StartDateTime = s.StartDateTime,
            EndDateTime = s.EndDateTime,
            TicketPrice = s.TicketPrice
        };

        ViewBag.Movies = db.Movies.ToList();
        ViewBag.Halls = db.Halls.OrderBy(h => h.Id).ToList();
        ViewBag.Cinemas = db.Cinemas.OrderBy(c => c.Id).ToList();
        return View(vm);
    }

    //POST: Admin/EditShowtime
    [HttpPost]
    public IActionResult EditShowtime(ShowtimeVM vm)
    {
        var s = db.Showtimes.Find(vm.Id);

        if (s == null)
        {
            TempData["Error"] = "Movie not found.";
            return RedirectToAction(nameof(ShowtimeManage));
        }

        if (ModelState.IsValid)
        {
            var movie = db.Movies.FirstOrDefault(m => m.Id == vm.MovieId);
            if (movie == null)
            {
                ModelState.AddModelError("MovieId", "Selected movie not found.");
                ViewBag.Movies = db.Movies.ToList();
                ViewBag.Halls = db.Halls.OrderBy(h => h.Id).ToList();
                ViewBag.Cinemas = db.Cinemas.OrderBy(c => c.Id).ToList();
                return View(vm);
            }
            s.HallId = vm.HallId;
            s.StartDateTime = vm.StartDateTime;
            s.EndDateTime = s.StartDateTime.AddMinutes(movie.RunningTime);
            s.TicketPrice = vm.TicketPrice;

            db.SaveChanges();

            TempData["Info"] = "Movie Showtime updated successfully";
            return RedirectToAction(nameof(ShowtimeManage));
        }
        ViewBag.Movies = db.Movies.ToList();
        ViewBag.Halls = db.Halls.OrderBy(h => h.Id).ToList();
        ViewBag.Cinemas = db.Cinemas.OrderBy(c => c.Id).ToList();
        return View(vm);
    }
}

