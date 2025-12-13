using Main.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    // GET: Admin/EditFoodItem/5
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
}
