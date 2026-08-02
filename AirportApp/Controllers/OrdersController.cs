using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AirportApp.Models;
using Microsoft.AspNetCore.Authorization;

namespace AirportApp.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly AirportDbContext _context;

        public OrdersController(AirportDbContext context)
        {
            _context = context;
        }

        // GET: Orders
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? pageNumber, int? pageSize)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["DateSortParm"] = String.IsNullOrEmpty(sortOrder) ? "date_desc" : "";
            ViewData["AmountSortParm"] = sortOrder == "Amount" ? "amount_desc" : "Amount";
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

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Administrador");
            var truncatedUserId = userId?.Substring(0, Math.Min(20, userId.Length)) ?? "";

            var airportDbContext = _context.Orders
                .Include(o => o.BookRefNavigation)
                .ThenInclude(b => b.Tickets)
                .AsNoTracking();

            if (!isAdmin)
            {
                airportDbContext = airportDbContext.Where(o => o.BookRefNavigation.Tickets.Any(t => t.PassengerId == truncatedUserId));
            }

            if (!String.IsNullOrEmpty(searchString))
            {
                var lowerSearch = searchString.Trim().ToLower();
                if (lowerSearch == "aprobado" || lowerSearch == "completado" || lowerSearch == "completed")
                {
                    airportDbContext = airportDbContext.Where(s => s.Status.Contains("Completed") || s.Status.Contains("Aprobado") || s.Status.Contains("Completado"));
                }
                else if (lowerSearch == "pendiente" || lowerSearch == "pending")
                {
                    airportDbContext = airportDbContext.Where(s => s.Status.Contains("Pending") || s.Status.Contains("Pendiente"));
                }
                else if (lowerSearch == "rechazado" || lowerSearch == "declined" || lowerSearch == "denied")
                {
                    airportDbContext = airportDbContext.Where(s => s.Status.Contains("Rechazado") || s.Status.Contains("Declined") || s.Status.Contains("Denied"));
                }
                else if (lowerSearch == "reembolsado" || lowerSearch == "refunded")
                {
                    airportDbContext = airportDbContext.Where(s => s.Status.Contains("Refunded") || s.Status.Contains("Reembolsado"));
                }
                else if (lowerSearch == "fallido" || lowerSearch == "failed")
                {
                    airportDbContext = airportDbContext.Where(s => s.Status.Contains("Failed") || s.Status.Contains("Fallido"));
                }
                else if (lowerSearch == "cancelado" || lowerSearch == "cancelled" || lowerSearch == "canceled")
                {
                    airportDbContext = airportDbContext.Where(s => s.Status.Contains("Cancelled") || s.Status.Contains("Canceled") || s.Status.Contains("Cancelado"));
                }
                else
                {
                    airportDbContext = airportDbContext.Where(s => s.Status.Contains(searchString));
                }
            }

            switch (sortOrder)
            {
                case "date_desc":
                    airportDbContext = airportDbContext.OrderByDescending(s => s.OrderDate);
                    break;
                case "Amount":
                    airportDbContext = airportDbContext.OrderBy(s => s.TotalAmount);
                    break;
                case "amount_desc":
                    airportDbContext = airportDbContext.OrderByDescending(s => s.TotalAmount);
                    break;
                case "Status":
                    airportDbContext = airportDbContext.OrderBy(s => s.Status);
                    break;
                case "status_desc":
                    airportDbContext = airportDbContext.OrderByDescending(s => s.Status);
                    break;
                default:
                    airportDbContext = airportDbContext.OrderBy(s => s.OrderDate);
                    break;
            }

            int pSize = pageSize ?? 10;
            return View(await PaginatedList<Order>.CreateAsync(airportDbContext, pageNumber ?? 1, pSize));
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Administrador");
            var truncatedUserId = userId?.Substring(0, Math.Min(20, userId.Length)) ?? "";

            var order = await _context.Orders
                .Include(o => o.BookRefNavigation)
                .ThenInclude(b => b.Tickets)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            if (!isAdmin && !order.BookRefNavigation.Tickets.Any(t => t.PassengerId == truncatedUserId))
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Orders/Create
        public IActionResult Create()
        {
            ViewData["BookRef"] = new SelectList(_context.Bookings, "BookRef", "BookRef");
            return View();
        }

        // POST: Orders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OrderId,BookRef,OrderDate,TotalAmount,Status")] Order order)
        {
            if (ModelState.IsValid)
            {
                _context.Add(order);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BookRef"] = new SelectList(_context.Bookings, "BookRef", "BookRef", order.BookRef);
            return View(order);
        }

        // GET: Orders/Edit/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            ViewData["BookRef"] = new SelectList(_context.Bookings, "BookRef", "BookRef", order.BookRef);
            return View(order);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int id, [Bind("OrderId,BookRef,OrderDate,TotalAmount,Status")] Order order)
        {
            if (id != order.OrderId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.OrderId))
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
            ViewData["BookRef"] = new SelectList(_context.Bookings, "BookRef", "BookRef", order.BookRef);
            return View(order);
        }

        // GET: Orders/Delete/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.BookRefNavigation)
                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderId == id);
        }
    }
}
