using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AirportApp.Models;
using AirportApp.Services.Payments;
using Microsoft.AspNetCore.Authorization;

namespace AirportApp.Controllers
{
    [Authorize]
    public class FlightReprogrammingController : Controller
    {
        private readonly AirportDbContext _context;
        private readonly PayPalService _payPalService;
        private readonly PayPhoneApiLinkService _payPhoneService;

        public FlightReprogrammingController(AirportDbContext context, PayPalService payPalService, PayPhoneApiLinkService payPhoneService)
        {
            _context = context;
            _payPalService = payPalService;
            _payPhoneService = payPhoneService;
        }

        // Diagnostic action to find bookings with ticket flights
        [AllowAnonymous]
        public async Task<IActionResult> Diagnose()
        {
            var bookingsWithFlights = await _context.Bookings
                .AsNoTracking()
                .Where(b => b.Tickets.Any(t => t.TicketFlights.Any()))
                .Select(b => new { b.BookRef, TicketsCount = b.Tickets.Count })
                .Take(10)
                .ToListAsync();

            return Json(bookingsWithFlights);
        }

        // 1. Search Booking (GET)
        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        // Search Booking (POST)
        [HttpPost]
        [AllowAnonymous]
        public IActionResult Search(string bookRef)
        {
            if (string.IsNullOrEmpty(bookRef))
            {
                TempData["Error"] = "Por favor, ingrese un código de reserva.";
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(BookingTickets), new { bookRef = bookRef.ToUpper() });
        }

        // Display Booking Tickets (GET)
        [HttpGet]
        public async Task<IActionResult> BookingTickets(string bookRef)
        {
            if (string.IsNullOrEmpty(bookRef))
            {
                return RedirectToAction(nameof(Index));
            }

            var booking = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Tickets)
                .FirstOrDefaultAsync(b => b.BookRef == bookRef);

            if (booking == null)
            {
                TempData["Error"] = "Reserva no encontrada.";
                return RedirectToAction(nameof(Index));
            }

            return View(booking);
        }

        // 2. Select Ticket and show its flights (GET)
        public async Task<IActionResult> SelectTicket(string ticketNo)
        {
            var ticket = await _context.Tickets
                .AsNoTracking()
                .Include(t => t.TicketFlights)
                    .ThenInclude(tf => tf.Flight)
                .FirstOrDefaultAsync(t => t.TicketNo == ticketNo);

            if (ticket == null)
            {
                return NotFound("Boleto no encontrado.");
            }

            return View(ticket);
        }

        // 3. Search Alternative Flights (GET)
        public async Task<IActionResult> SearchFlights(string ticketNo, int currentFlightId)
        {
            var currentFlight = await _context.Flights.AsNoTracking().FirstOrDefaultAsync(f => f.FlightId == currentFlightId);
            if (currentFlight == null) return NotFound("Vuelo actual no encontrado.");

            // Find origin and destination of the current flight using Timetables view
            var currentTimetable = await _context.Timetables
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.FlightId == currentFlightId);

            if (currentTimetable == null || string.IsNullOrEmpty(currentTimetable.DepartureAirport) || string.IsNullOrEmpty(currentTimetable.ArrivalAirport))
            {
                return BadRequest("No se pudo determinar el origen y destino del vuelo actual.");
            }

            // Search scheduled alternative flights for the same route
            var alternatives = await _context.Timetables
                .AsNoTracking()
                .Where(t => t.DepartureAirport == currentTimetable.DepartureAirport 
                            && t.ArrivalAirport == currentTimetable.ArrivalAirport 
                            && t.FlightId != currentFlightId
                            && (t.Status == "Scheduled" || t.Status == "On Time"))
                .OrderBy(t => t.ScheduledDeparture)
                .Take(20)
                .ToListAsync();

            ViewData["TicketNo"] = ticketNo;
            ViewData["CurrentFlightId"] = currentFlightId;
            ViewData["DepartureAirportName"] = currentTimetable.DepartureAirport;
            ViewData["ArrivalAirportName"] = currentTimetable.ArrivalAirport;

            return View(alternatives);
        }

        // 4. Compare (GET)
        public async Task<IActionResult> Compare(string ticketNo, int currentFlightId, int newFlightId)
        {
            var ticketFlight = await _context.TicketFlights
                .AsNoTracking()
                .Include(tf => tf.TicketNoNavigation)
                .FirstOrDefaultAsync(tf => tf.TicketNo == ticketNo && tf.FlightId == currentFlightId);

            if (ticketFlight == null) return NotFound("Detalles del boleto no encontrados.");

            var currentFlight = await _context.Timetables.AsNoTracking().FirstOrDefaultAsync(t => t.FlightId == currentFlightId);
            var newFlight = await _context.Timetables.AsNoTracking().FirstOrDefaultAsync(t => t.FlightId == newFlightId);

            if (currentFlight == null || newFlight == null) return NotFound("Vuelo no encontrado.");

            // Server-side calculation of the new fare and penalty
            decimal originalPrice = ticketFlight.Amount;
            decimal newPrice = await GetBasePriceForFlightAsync(newFlightId);
            decimal fareDifference = Math.Max(0, newPrice - originalPrice);
            decimal penalty = 50.00m;
            decimal totalDue = fareDifference + penalty;

            ViewData["TicketNo"] = ticketNo;
            ViewData["CurrentFlightId"] = currentFlightId;
            ViewData["NewFlightId"] = newFlightId;
            ViewData["OriginalPrice"] = originalPrice;
            ViewData["NewPrice"] = newPrice;
            ViewData["FareDifference"] = fareDifference;
            ViewData["Penalty"] = penalty;
            ViewData["TotalDue"] = totalDue;

            ViewData["CurrentFlight"] = currentFlight;
            ViewData["NewFlight"] = newFlight;
            ViewData["BookRef"] = ticketFlight.TicketNoNavigation.BookRef;

            return View();
        }

        // 5. Create Order and redirect to PayPal or PayPhone (POST)
        [HttpPost]
        public async Task<IActionResult> CreateOrder(string ticketNo, int currentFlightId, int newFlightId, string provider)
        {
            var ticketFlight = await _context.TicketFlights
                .AsNoTracking()
                .Include(tf => tf.TicketNoNavigation)
                .FirstOrDefaultAsync(tf => tf.TicketNo == ticketNo && tf.FlightId == currentFlightId);

            if (ticketFlight == null) return NotFound("Reserva no encontrada.");

            // Server-side calculation
            decimal originalPrice = ticketFlight.Amount;
            decimal newPrice = await GetBasePriceForFlightAsync(newFlightId);
            decimal fareDifference = Math.Max(0, newPrice - originalPrice);
            decimal penalty = 50.00m;
            decimal totalDue = fareDifference + penalty;

            string bookRef = ticketFlight.TicketNoNavigation.BookRef;

            // 1. Create Order
            var order = new Order
            {
                BookRef = bookRef,
                OrderDate = DateTime.UtcNow,
                TotalAmount = totalDue,
                Status = "Pendiente"
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // 2. Create OrderDetails
            var detail = new OrderDetail
            {
                OrderId = order.OrderId,
                Description = $"Reprogramación de boleto #{ticketNo} del vuelo {currentFlightId} al vuelo {newFlightId}. Penalidad ($50.00) + Diferencia tarifaria.",
                Amount = totalDue
            };
            _context.OrderDetails.Add(detail);

            // 3. Create FlightChangeRequest
            var request = new FlightChangeRequest
            {
                BookRef = bookRef,
                RequestedFlightId = newFlightId,
                RequestDate = DateTime.UtcNow,
                Status = "Pendiente"
            };
            _context.FlightChangeRequests.Add(request);
            await _context.SaveChangesAsync();

            string externalId = string.Empty;
            string paymentUrl = string.Empty;

            if (provider == "PayPhone")
            {
                // PayPhone Link integration
                string clientTransactionId = DateTime.Now.ToString("yyMMddHHmmssfff")[..15];
                string reference = $"Reprogramacion #{order.OrderId}";
                
                paymentUrl = await _payPhoneService.CreatePaymentLinkAsync(totalDue, clientTransactionId, reference);
                externalId = clientTransactionId;
            }
            else
            {
                // PayPal (Standard or Embedded)
                string scheme = Request.Scheme;
                string host = Request.Host.Value ?? "localhost";
                string returnUrl = $"{scheme}://{host}/Payment/Success";
                string cancelUrl = $"{scheme}://{host}/Payment/Cancel";

                var paypalResult = await _payPalService.CreateOrderAsync(
                    totalDue,
                    $"Reprogramacion #{order.OrderId}",
                    returnUrl,
                    cancelUrl
                );

                paymentUrl = paypalResult.ApprovalUrl;
                externalId = paypalResult.OrderId; // PayPal Order ID
            }

            // 5. Create Payment
            var payment = new Payment
            {
                OrderId = order.OrderId,
                PaymentDate = DateTime.UtcNow,
                Amount = totalDue,
                Status = "Pendiente",
                Gateway = provider,
                ExternalTransactionId = externalId,
                Currency = "USD",
                UserId = User.Identity?.Name ?? "Usuario",
                ResponseMessage = paymentUrl // Storing the payment link here
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Save details to Session/TempData to retrieve in Callback
            TempData["Reschedule_TicketNo"] = ticketNo;
            TempData["Reschedule_OldFlightId"] = currentFlightId.ToString();
            TempData["Reschedule_NewFlightId"] = newFlightId.ToString();
            TempData["Reschedule_RequestId"] = request.RequestId.ToString();

            if (provider == "PayPhone")
            {
                return RedirectToAction("PayPhoneCheckout", "Payment", new { paymentId = payment.PaymentId });
            }
            if (provider == "PayPalEmbed")
            {
                return RedirectToAction("PayPalEmbed", "Payment", new { paymentId = payment.PaymentId });
            }

            return Redirect(paymentUrl);
        }

        private async Task<decimal> GetBasePriceForFlightAsync(int flightId)
        {
            var avg = await _context.TicketFlights
                .Where(tf => tf.FlightId == flightId)
                .Select(tf => (decimal?)tf.Amount)
                .AverageAsync();

            return avg ?? 250.00m; // Fallback price
        }
    }
}
