using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AirportApp.Models;

namespace AirportApp.Controllers
{
    public class FlightChangeRequestsController : Controller
    {
        private readonly AirportDbContext _context;

        public FlightChangeRequestsController(AirportDbContext context)
        {
            _context = context;
        }

        // GET: FlightChangeRequests
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? pageNumber, int? pageSize)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["DateSortParm"] = String.IsNullOrEmpty(sortOrder) ? "date_desc" : "";
            ViewData["StatusSortParm"] = sortOrder == "Status" ? "status_desc" : "Status";

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

            var airportDbContext = _context.FlightChangeRequests
                .Include(f => f.BookRefNavigation)
                .Include(f => f.RequestedFlight)
                .AsNoTracking();

            if (!String.IsNullOrEmpty(searchString))
            {
                airportDbContext = airportDbContext.Where(s => s.Status.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "date_desc":
                    airportDbContext = airportDbContext.OrderByDescending(s => s.RequestDate);
                    break;
                case "Status":
                    airportDbContext = airportDbContext.OrderBy(s => s.Status);
                    break;
                case "status_desc":
                    airportDbContext = airportDbContext.OrderByDescending(s => s.Status);
                    break;
                default:
                    airportDbContext = airportDbContext.OrderBy(s => s.RequestDate);
                    break;
            }

            int pSize = pageSize ?? 10;
            return View(await PaginatedList<FlightChangeRequest>.CreateAsync(airportDbContext, pageNumber ?? 1, pSize));
        }

        // GET: FlightChangeRequests/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var flightChangeRequest = await _context.FlightChangeRequests
                .Include(f => f.BookRefNavigation)
                .Include(f => f.RequestedFlight)
                .FirstOrDefaultAsync(m => m.RequestId == id);
            if (flightChangeRequest == null)
            {
                return NotFound();
            }

            return View(flightChangeRequest);
        }

        // GET: FlightChangeRequests/Create
        public IActionResult Create()
        {
            ViewData["BookRef"] = new SelectList(_context.Bookings, "BookRef", "BookRef");
            ViewData["RequestedFlightId"] = new SelectList(_context.Flights, "FlightId", "FlightId");
            return View();
        }

        // POST: FlightChangeRequests/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RequestId,BookRef,RequestedFlightId,RequestDate,Status")] FlightChangeRequest flightChangeRequest)
        {
            if (ModelState.IsValid)
            {
                _context.Add(flightChangeRequest);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BookRef"] = new SelectList(_context.Bookings, "BookRef", "BookRef", flightChangeRequest.BookRef);
            ViewData["RequestedFlightId"] = new SelectList(_context.Flights, "FlightId", "FlightId", flightChangeRequest.RequestedFlightId);
            return View(flightChangeRequest);
        }

        // GET: FlightChangeRequests/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var flightChangeRequest = await _context.FlightChangeRequests.FindAsync(id);
            if (flightChangeRequest == null)
            {
                return NotFound();
            }
            ViewData["BookRef"] = new SelectList(_context.Bookings, "BookRef", "BookRef", flightChangeRequest.BookRef);
            ViewData["RequestedFlightId"] = new SelectList(_context.Flights, "FlightId", "FlightId", flightChangeRequest.RequestedFlightId);
            return View(flightChangeRequest);
        }

        // POST: FlightChangeRequests/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RequestId,BookRef,RequestedFlightId,RequestDate,Status")] FlightChangeRequest flightChangeRequest)
        {
            if (id != flightChangeRequest.RequestId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(flightChangeRequest);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FlightChangeRequestExists(flightChangeRequest.RequestId))
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
            ViewData["BookRef"] = new SelectList(_context.Bookings, "BookRef", "BookRef", flightChangeRequest.BookRef);
            ViewData["RequestedFlightId"] = new SelectList(_context.Flights, "FlightId", "FlightId", flightChangeRequest.RequestedFlightId);
            return View(flightChangeRequest);
        }

        // GET: FlightChangeRequests/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var flightChangeRequest = await _context.FlightChangeRequests
                .Include(f => f.BookRefNavigation)
                .Include(f => f.RequestedFlight)
                .FirstOrDefaultAsync(m => m.RequestId == id);
            if (flightChangeRequest == null)
            {
                return NotFound();
            }

            return View(flightChangeRequest);
        }

        // POST: FlightChangeRequests/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var flightChangeRequest = await _context.FlightChangeRequests.FindAsync(id);
            if (flightChangeRequest != null)
            {
                _context.FlightChangeRequests.Remove(flightChangeRequest);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FlightChangeRequestExists(int id)
        {
            return _context.FlightChangeRequests.Any(e => e.RequestId == id);
        }
    }
}
