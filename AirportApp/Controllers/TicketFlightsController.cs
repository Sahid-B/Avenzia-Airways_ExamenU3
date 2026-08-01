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
    public class TicketFlightsController : Controller
    {
        private readonly AirportDbContext _context;

        public TicketFlightsController(AirportDbContext context)
        {
            _context = context;
        }

        // GET: TicketFlights
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? pageNumber, int? pageSize)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["FareSortParm"] = String.IsNullOrEmpty(sortOrder) ? "fare_desc" : "";
            ViewData["AmountSortParm"] = sortOrder == "Amount" ? "amount_desc" : "Amount";

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

            var airportDbContext = _context.TicketFlights.Include(t => t.Flight).Include(t => t.TicketNoNavigation).AsNoTracking();

            if (!String.IsNullOrEmpty(searchString))
            {
                airportDbContext = airportDbContext.Where(s => s.FareConditions.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "fare_desc":
                    airportDbContext = airportDbContext.OrderByDescending(s => s.FareConditions);
                    break;
                case "Amount":
                    airportDbContext = airportDbContext.OrderBy(s => s.Amount);
                    break;
                case "amount_desc":
                    airportDbContext = airportDbContext.OrderByDescending(s => s.Amount);
                    break;
                default:
                    airportDbContext = airportDbContext.OrderBy(s => s.FareConditions);
                    break;
            }

            int pSize = pageSize ?? 10;
            return View(await PaginatedList<TicketFlight>.CreateAsync(airportDbContext, pageNumber ?? 1, pSize));
        }

        // GET: TicketFlights/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticketFlight = await _context.TicketFlights
                .Include(t => t.Flight)
                .Include(t => t.TicketNoNavigation)
                .FirstOrDefaultAsync(m => m.TicketNo == id);
            if (ticketFlight == null)
            {
                return NotFound();
            }

            return View(ticketFlight);
        }

        // GET: TicketFlights/Create
        public IActionResult Create()
        {
            ViewData["FlightId"] = new SelectList(_context.Flights, "FlightId", "FlightId");
            ViewData["TicketNo"] = new SelectList(_context.Tickets, "TicketNo", "TicketNo");
            return View();
        }

        // POST: TicketFlights/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TicketNo,FlightId,FareConditions,Amount")] TicketFlight ticketFlight)
        {
            if (ModelState.IsValid)
            {
                _context.Add(ticketFlight);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["FlightId"] = new SelectList(_context.Flights, "FlightId", "FlightId", ticketFlight.FlightId);
            ViewData["TicketNo"] = new SelectList(_context.Tickets, "TicketNo", "TicketNo", ticketFlight.TicketNo);
            return View(ticketFlight);
        }

        // GET: TicketFlights/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticketFlight = await _context.TicketFlights.FindAsync(id);
            if (ticketFlight == null)
            {
                return NotFound();
            }
            ViewData["FlightId"] = new SelectList(_context.Flights, "FlightId", "FlightId", ticketFlight.FlightId);
            ViewData["TicketNo"] = new SelectList(_context.Tickets, "TicketNo", "TicketNo", ticketFlight.TicketNo);
            return View(ticketFlight);
        }

        // POST: TicketFlights/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("TicketNo,FlightId,FareConditions,Amount")] TicketFlight ticketFlight)
        {
            if (id != ticketFlight.TicketNo)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ticketFlight);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TicketFlightExists(ticketFlight.TicketNo))
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
            ViewData["FlightId"] = new SelectList(_context.Flights, "FlightId", "FlightId", ticketFlight.FlightId);
            ViewData["TicketNo"] = new SelectList(_context.Tickets, "TicketNo", "TicketNo", ticketFlight.TicketNo);
            return View(ticketFlight);
        }

        // GET: TicketFlights/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticketFlight = await _context.TicketFlights
                .Include(t => t.Flight)
                .Include(t => t.TicketNoNavigation)
                .FirstOrDefaultAsync(m => m.TicketNo == id);
            if (ticketFlight == null)
            {
                return NotFound();
            }

            return View(ticketFlight);
        }

        // POST: TicketFlights/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var ticketFlight = await _context.TicketFlights.FindAsync(id);
            if (ticketFlight != null)
            {
                _context.TicketFlights.Remove(ticketFlight);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TicketFlightExists(string id)
        {
            return _context.TicketFlights.Any(e => e.TicketNo == id);
        }
    }
}
