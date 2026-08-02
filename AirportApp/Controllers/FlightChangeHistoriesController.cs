using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AirportApp.Models;

namespace AirportApp.Controllers
{
    [Authorize]
    public class FlightChangeHistoriesController : Controller
    {
        private readonly AirportDbContext _context;

        public FlightChangeHistoriesController(AirportDbContext context)
        {
            _context = context;
        }

        // GET: FlightChangeHistories
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? pageNumber, int? pageSize)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["DateSortParm"] = String.IsNullOrEmpty(sortOrder) ? "date_desc" : "";
            ViewData["ReasonSortParm"] = sortOrder == "Reason" ? "reason_desc" : "Reason";

            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;
            ViewData["PageSize"] = pageSize ?? 10;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Administrador");
            var truncatedUserId = userId?.Substring(0, Math.Min(20, userId.Length)) ?? "";

            var airportDbContext = _context.FlightChangeHistories
                .Include(f => f.BookRefNavigation)
                .Include(f => f.NewFlight)
                .Include(f => f.OldFlight)
                .AsNoTracking();

            if (!isAdmin)
            {
                airportDbContext = airportDbContext.Where(f => f.BookRefNavigation.Tickets.Any(t => t.PassengerId == truncatedUserId));
            }

            if (!String.IsNullOrEmpty(searchString))
            {
                airportDbContext = airportDbContext.Where(s => s.Reason.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "date_desc":
                    airportDbContext = airportDbContext.OrderByDescending(s => s.ChangeDate);
                    break;
                case "Reason":
                    airportDbContext = airportDbContext.OrderBy(s => s.Reason);
                    break;
                case "reason_desc":
                    airportDbContext = airportDbContext.OrderByDescending(s => s.Reason);
                    break;
                default:
                    airportDbContext = airportDbContext.OrderBy(s => s.ChangeDate);
                    break;
            }

            int pSize = pageSize ?? 10;
            return View(await PaginatedList<FlightChangeHistory>.CreateAsync(airportDbContext, pageNumber ?? 1, pSize));
        }

        // GET: FlightChangeHistories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Administrador");
            var truncatedUserId = userId?.Substring(0, Math.Min(20, userId.Length)) ?? "";

            var flightChangeHistory = await _context.FlightChangeHistories
                .Include(f => f.BookRefNavigation)
                    .ThenInclude(b => b.Tickets)
                .Include(f => f.NewFlight)
                .Include(f => f.OldFlight)
                .FirstOrDefaultAsync(m => m.ChangeId == id);

            if (flightChangeHistory == null)
            {
                return NotFound();
            }

            if (!isAdmin)
            {
                var belongsToUser = flightChangeHistory.BookRefNavigation.Tickets.Any(t => t.PassengerId == truncatedUserId);
                if (!belongsToUser)
                {
                    return Forbid();
                }
            }

            return View(flightChangeHistory);
        }

        // GET: FlightChangeHistories/Create
        public IActionResult Create()
        {
            ViewData["BookRef"] = new SelectList(_context.Bookings, "BookRef", "BookRef");
            ViewData["NewFlightId"] = new SelectList(_context.Flights, "FlightId", "FlightId");
            ViewData["OldFlightId"] = new SelectList(_context.Flights, "FlightId", "FlightId");
            return View();
        }

        // POST: FlightChangeHistories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ChangeId,BookRef,OldFlightId,NewFlightId,ChangeDate,Reason")] FlightChangeHistory flightChangeHistory)
        {
            if (ModelState.IsValid)
            {
                _context.Add(flightChangeHistory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BookRef"] = new SelectList(_context.Bookings, "BookRef", "BookRef", flightChangeHistory.BookRef);
            ViewData["NewFlightId"] = new SelectList(_context.Flights, "FlightId", "FlightId", flightChangeHistory.NewFlightId);
            ViewData["OldFlightId"] = new SelectList(_context.Flights, "FlightId", "FlightId", flightChangeHistory.OldFlightId);
            return View(flightChangeHistory);
        }

        // GET: FlightChangeHistories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var flightChangeHistory = await _context.FlightChangeHistories.FindAsync(id);
            if (flightChangeHistory == null)
            {
                return NotFound();
            }
            ViewData["BookRef"] = new SelectList(_context.Bookings, "BookRef", "BookRef", flightChangeHistory.BookRef);
            ViewData["NewFlightId"] = new SelectList(_context.Flights, "FlightId", "FlightId", flightChangeHistory.NewFlightId);
            ViewData["OldFlightId"] = new SelectList(_context.Flights, "FlightId", "FlightId", flightChangeHistory.OldFlightId);
            return View(flightChangeHistory);
        }

        // POST: FlightChangeHistories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ChangeId,BookRef,OldFlightId,NewFlightId,ChangeDate,Reason")] FlightChangeHistory flightChangeHistory)
        {
            if (id != flightChangeHistory.ChangeId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(flightChangeHistory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FlightChangeHistoryExists(flightChangeHistory.ChangeId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["BookRef"] = new SelectList(_context.Bookings, "BookRef", "BookRef", flightChangeHistory.BookRef);
            ViewData["NewFlightId"] = new SelectList(_context.Flights, "FlightId", "FlightId", flightChangeHistory.NewFlightId);
            ViewData["OldFlightId"] = new SelectList(_context.Flights, "FlightId", "FlightId", flightChangeHistory.OldFlightId);
            return View(flightChangeHistory);
        }

        // GET: FlightChangeHistories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var flightChangeHistory = await _context.FlightChangeHistories
                .Include(f => f.BookRefNavigation)
                .Include(f => f.NewFlight)
                .Include(f => f.OldFlight)
                .FirstOrDefaultAsync(m => m.ChangeId == id);
            if (flightChangeHistory == null)
            {
                return NotFound();
            }

            return View(flightChangeHistory);
        }

        // POST: FlightChangeHistories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var flightChangeHistory = await _context.FlightChangeHistories.FindAsync(id);
            if (flightChangeHistory != null)
            {
                _context.FlightChangeHistories.Remove(flightChangeHistory);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FlightChangeHistoryExists(int id)
        {
            return _context.FlightChangeHistories.Any(e => e.ChangeId == id);
        }
    }
}
