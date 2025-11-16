using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Main.Controllers;

public class HomeController(DB db) : Controller
{
    // GET: Home/Index
    public IActionResult Index()
    {
        ViewBag.Members = db.Members;
        ViewBag.Types   = db.Types.OrderBy(t => t.Price);
        ViewBag.Rooms   = db.Rooms.OrderBy(rm => rm.Id);
        return View();
    }



    // ------------------------------------------------------------------------
    // Reservation
    // ------------------------------------------------------------------------

    // GET: Home/Reserve
    [Authorize(Roles = "Member")]
    public IActionResult Reserve()
    {
        var vm = new ReserveVM
        {
            // TODO
        };

        // TODO
        ViewBag.TypeList = new SelectList("");
        return View();
    }

    // POST: Home/Reserve
    [Authorize(Roles = "Member")]
    [HttpPost]
    public IActionResult Reserve(ReserveVM vm)
    {
        // Validation (1): CheckIn within 30 days range
        if (ModelState.IsValid("CheckIn"))
        {
            // TODO

            if (true)
            {
                ModelState.AddModelError("CheckIn", "Date out of range.");
            }
        }

        // Validation (2): CheckOut within 10 days range (after CheckIn)
        if (ModelState.IsValid("CheckIn") && ModelState.IsValid("CheckOut"))
        {
            // TODO

            if (true)
            {
                ModelState.AddModelError("CheckOut", "Date out of range.");
            }
        }
        
        if (ModelState.IsValid)
        {
            // 1. Get occupied rooms 
            // TODO

            // 2. Get first available room (filtered by room type)
            // TODO
            Room? room = null;

            // 3. Is room available?
            if (room == null)
            {
                ModelState.AddModelError("TypeId", "No room availble.");
            }
            else
            {
                // 4. Insert Reservation record
                // TODO
                var rs = new Reservation
                {

                };

                TempData["Info"] = "Room reserved.";
                return RedirectToAction("Detail", new { rs.Id });
            }
        }

        ViewBag.TypeList = new SelectList(db.Types.OrderBy(t => t.Price), "Id", "Name");
        return View(vm);
    }

    // GET: Home/List
    public IActionResult List()
    {
        var m = db.Reservations
                  .Include(rs => rs.Member)
                  .Include(rs => rs.Room)
                  .ThenInclude(rm => rm.Type);

        return View(m);
    }

    // POST: Home/Reset
    [HttpPost]
    public IActionResult Reset()
    {
        db.Database.ExecuteSqlRaw(@"
            TRUNCATE TABLE [Reservations];
        ");

        return RedirectToAction("List");
    }

    // GET: Home/Detail
    public IActionResult Detail(int id)
    {
        var m = db.Reservations
                  .Include(rs => rs.Member)
                  .Include(rs => rs.Room)
                  .ThenInclude(rm => rm.Type)
                  .FirstOrDefault(rs => rs.Id == id);

        if (m == null) 
        {
            return RedirectToAction("List");
        }

        return View(m);
    }

    // GET: Home/Status
    public IActionResult Status(DateOnly? month)
    {
        var m = month.GetValueOrDefault(DateTime.Today.ToDateOnly());

        // Min = First day of the month
        // Max = First day of next month
        
        // TODO

        // 1. Initialize dictionary
        // ------------------------
        // Dictionary<Room, List<DateOnly>>
        // Key   = Room object
        // Value = List of DateOnly objects
        //
        // dict[R001] = [2022-12-01, 2022-12-02, ...]
        // dict[R002] = [2022-12-03, 2022-12-04, ...]

        // TODO

        // 2. Retrieve reservation records
        // -------------------------------
        // Example: 2024-12-01 (min) ... 2025-01-01 (max)

        // TODO

        // 3. Fill the dictionary
        // ----------------------
        // Example: CheckIn = 2024-12-10, CheckOut = 2024-12-15
        // Entries --> 10, 11, 12, 13, 14 *** 15 not included ***

        // TODO

        // TODO
        return View();
    }
}
