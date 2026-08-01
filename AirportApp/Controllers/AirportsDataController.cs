using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AirportApp.Models;
using Microsoft.AspNetCore.Authorization;

namespace AirportApp.Controllers
{
    [Authorize]
    public class AirportsDataController : Controller
    {
        private readonly AirportDbContext _context;

        public AirportsDataController(AirportDbContext context)
        {
            _context = context;
        }

        // GET: AirportsData
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? pageNumber, int? pageSize)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["CitySortParm"] = sortOrder == "City" ? "city_desc" : "City";
            ViewData["CountrySortParm"] = sortOrder == "Country" ? "country_desc" : "Country";

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

            var airports = _context.AirportsData.AsNoTracking();

            if (!String.IsNullOrEmpty(searchString))
            {
                airports = airports.Where(s => s.AirportName.Contains(searchString) || s.City.Contains(searchString) || s.Country.Contains(searchString) || s.AirportCode.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "name_desc":
                    airports = airports.OrderByDescending(s => s.AirportName);
                    break;
                case "City":
                    airports = airports.OrderBy(s => s.City);
                    break;
                case "city_desc":
                    airports = airports.OrderByDescending(s => s.City);
                    break;
                case "Country":
                    airports = airports.OrderBy(s => s.Country);
                    break;
                case "country_desc":
                    airports = airports.OrderByDescending(s => s.Country);
                    break;
                default:
                    airports = airports.OrderBy(s => s.AirportName);
                    break;
            }

            int pSize = pageSize ?? 10;
            return View(await PaginatedList<AirportsDatum>.CreateAsync(airports, pageNumber ?? 1, pSize));
        }

        // GET: AirportsData/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var airportsDatum = await _context.AirportsData
                .FirstOrDefaultAsync(m => m.AirportCode == id);
            if (airportsDatum == null)
            {
                return NotFound();
            }

            return View(airportsDatum);
        }

        // GET: AirportsData/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: AirportsData/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AirportCode,AirportName,City,Country,Coordinates,Timezone")] AirportsDatum airportsDatum)
        {
            if (ModelState.IsValid)
            {
                _context.Add(airportsDatum);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(airportsDatum);
        }

        // GET: AirportsData/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var airportsDatum = await _context.AirportsData.FindAsync(id);
            if (airportsDatum == null)
            {
                return NotFound();
            }
            return View(airportsDatum);
        }

        // POST: AirportsData/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("AirportCode,AirportName,City,Country,Coordinates,Timezone")] AirportsDatum airportsDatum)
        {
            if (id != airportsDatum.AirportCode)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(airportsDatum);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AirportsDatumExists(airportsDatum.AirportCode))
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
            return View(airportsDatum);
        }

        // GET: AirportsData/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var airportsDatum = await _context.AirportsData
                .FirstOrDefaultAsync(m => m.AirportCode == id);
            if (airportsDatum == null)
            {
                return NotFound();
            }

            return View(airportsDatum);
        }

        // POST: AirportsData/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var airportsDatum = await _context.AirportsData.FindAsync(id);
            if (airportsDatum != null)
            {
                _context.AirportsData.Remove(airportsDatum);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AirportsDatumExists(string id)
        {
            return _context.AirportsData.Any(e => e.AirportCode == id);
        }
    }
}
