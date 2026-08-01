using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AirportApp.Models;
using static AirportApp.Models.LinqQueriesViewModel;

namespace AirportApp.Controllers
{
    public class LinqQueriesController : Controller
    {
        private readonly AirportDbContext _context;

        public LinqQueriesController(AirportDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string searchBookRef,
            DateTime? filterDate,
            decimal? minAmount,
            int? pageNumber)
        {
            var vm = new LinqQueriesViewModel
            {
                SearchBookRef = searchBookRef,
                FilterDate = filterDate,
                MinAmount = minAmount
            };

            // ==========================================
            // CONSULTA 1: Búsqueda, Filtros (2), Include, Paginación, Select
            // ==========================================
            int pageSize = 5;
            IQueryable<Booking> bookingsQuery = _context.Bookings
                .AsNoTracking()
                .Include(b => b.Tickets); 

            // Filtro por texto
            if (!string.IsNullOrEmpty(searchBookRef))
            {
                bookingsQuery = bookingsQuery.Where(b => b.BookRef.Contains(searchBookRef));
            }
            
            // Filtro 1: Fecha
            if (filterDate.HasValue)
            {
                bookingsQuery = bookingsQuery.Where(b => b.BookDate.Date >= filterDate.Value.Date);
            }

            // Filtro 2: Monto mínimo
            if (minAmount.HasValue)
            {
                bookingsQuery = bookingsQuery.Where(b => b.TotalAmount >= minAmount.Value);
            }

            // Ordenamiento
            bookingsQuery = bookingsQuery.OrderByDescending(b => b.BookDate);

            // Paginación y Select
            int totalBookings = await bookingsQuery.CountAsync();
            var items = await bookingsQuery
                .Skip(((pageNumber ?? 1) - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BookingProjection
                {
                    BookRef = b.BookRef,
                    BookDate = b.BookDate,
                    TotalAmount = b.TotalAmount,
                    TicketsCount = b.Tickets.Count
                })
                .ToListAsync();

            vm.Bookings = new PaginatedList<BookingProjection>(items, totalBookings, pageNumber ?? 1, pageSize);

            // ==========================================
            // CONSULTA 2: GroupBy, Count, Sum, Average
            // ==========================================
            var rawStats = await _context.Payments
                .AsNoTracking()
                .GroupBy(p => p.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount),
                })
                .ToListAsync();

            vm.PaymentStatistics = rawStats
                .Select(s => new
                {
                    NormalizedStatus = (s.Status ?? "").ToLower() switch
                    {
                        "completed" or "aprobado" => "Aprobado",
                        "pending" or "pendiente" => "Pendiente",
                        "failed" or "fallido" or "rechazado" => "Fallido",
                        "refunded" or "reembolsado" => "Reembolsado",
                        _ => "Desconocido"
                    },
                    s.Count,
                    s.TotalAmount
                })
                .GroupBy(x => x.NormalizedStatus)
                .Select(g => new LinqQueriesViewModel.PaymentStats
                {
                    Status = g.Key,
                    Count = g.Sum(x => x.Count),
                    TotalAmount = g.Sum(x => x.TotalAmount),
                    AverageAmount = g.Sum(x => x.Count) > 0 ? g.Sum(x => x.TotalAmount) / g.Sum(x => x.Count) : 0
                })
                .OrderBy(s => s.Status)
                .ToList();

            // ==========================================
            // CONSULTA 3: OrderBy, Select, Take
            // ==========================================
            vm.TopAirports = await _context.AirportsData
                .AsNoTracking()
                .OrderBy(a => a.City)
                .Take(5)
                .Select(a => new AirportProjection
                {
                    AirportCode = a.AirportCode,
                    AirportName = a.AirportName,
                    City = a.City
                })
                .ToListAsync();

            // ==========================================
            // CONSULTA 4: Any
            // ==========================================
            vm.HasPendingPayments = await _context.Payments
                .AsNoTracking()
                .AnyAsync(p => p.Status == "Pending");

            return View(vm);
        }
    }
}
