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
    public class TicketsController : Controller
    {
        private readonly AirportDbContext _context;

        public TicketsController(AirportDbContext context)
        {
            _context = context;
        }

        // GET: Tickets
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? pageNumber, int? pageSize)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["IdSortParm"] = String.IsNullOrEmpty(sortOrder) ? "id_desc" : "";
            ViewData["NameSortParm"] = sortOrder == "Name" ? "name_desc" : "Name";

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

            var airportDbContext = _context.Tickets.Include(t => t.BookRefNavigation).AsNoTracking();

            if (!String.IsNullOrEmpty(searchString))
            {
                airportDbContext = airportDbContext.Where(s => s.PassengerName.Contains(searchString) || s.PassengerId.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "id_desc":
                    airportDbContext = airportDbContext.OrderByDescending(s => s.PassengerId);
                    break;
                case "Name":
                    airportDbContext = airportDbContext.OrderBy(s => s.PassengerName);
                    break;
                case "name_desc":
                    airportDbContext = airportDbContext.OrderByDescending(s => s.PassengerName);
                    break;
                default:
                    airportDbContext = airportDbContext.OrderBy(s => s.PassengerId);
                    break;
            }

            int pSize = pageSize ?? 10;
            return View(await PaginatedList<Ticket>.CreateAsync(airportDbContext, pageNumber ?? 1, pSize));
        }

        // GET: Tickets/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets
                .Include(t => t.BookRefNavigation)
                .FirstOrDefaultAsync(m => m.TicketNo == id);
            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        // GET: Tickets/Create
        public IActionResult Create()
        {
            ViewData["BookRef"] = new SelectList(_context.Bookings, "BookRef", "BookRef");
            return View();
        }

        // POST: Tickets/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TicketNo,BookRef,PassengerId,PassengerName,Outbound")] Ticket ticket)
        {
            if (ModelState.IsValid)
            {
                _context.Add(ticket);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BookRef"] = new SelectList(_context.Bookings, "BookRef", "BookRef", ticket.BookRef);
            return View(ticket);
        }

        // GET: Tickets/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }
            ViewData["BookRef"] = new SelectList(_context.Bookings, "BookRef", "BookRef", ticket.BookRef);
            return View(ticket);
        }

        // POST: Tickets/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("TicketNo,BookRef,PassengerId,PassengerName,Outbound")] Ticket ticket)
        {
            if (id != ticket.TicketNo)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ticket);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TicketExists(ticket.TicketNo))
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
            ViewData["BookRef"] = new SelectList(_context.Bookings, "BookRef", "BookRef", ticket.BookRef);
            return View(ticket);
        }

        // GET: Tickets/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets
                .Include(t => t.BookRefNavigation)
                .FirstOrDefaultAsync(m => m.TicketNo == id);
            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        // POST: Tickets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket != null)
            {
                _context.Tickets.Remove(ticket);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TicketExists(string id)
        {
            return _context.Tickets.Any(e => e.TicketNo == id);
        }
    }
}
