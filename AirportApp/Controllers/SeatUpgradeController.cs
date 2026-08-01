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
    public class SeatUpgradeController : Controller
    {
        private readonly AirportDbContext _context;
        private readonly PayPalService _payPalService;

        public SeatUpgradeController(AirportDbContext context, PayPalService payPalService)
        {
            _context = context;
            _payPalService = payPalService;
        }

        // 1. Search Booking (GET)
        public IActionResult Index()
        {
            return View();
        }

        // Search Booking (POST)
        [HttpPost]
        public IActionResult Search(string bookRef)
        {
            if (string.IsNullOrEmpty(bookRef))
            {
                TempData["Error"] = "Por favor, ingrese un código de reserva.";
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(BookingTickets), new { bookRef = bookRef.ToUpper() });
        }

        // 2. Display Booking Tickets (GET)
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
                    .ThenInclude(t => t.TicketFlights)
                        .ThenInclude(tf => tf.Flight)
                .FirstOrDefaultAsync(b => b.BookRef == bookRef);

            if (booking == null)
            {
                TempData["Error"] = "Reserva no encontrada.";
                return RedirectToAction(nameof(Index));
            }

            return View(booking);
        }

        // 3. Confirm Upgrade (GET)
        public async Task<IActionResult> ConfirmUpgrade(string ticketNo, int flightId)
        {
            var ticketFlight = await _context.TicketFlights
                .AsNoTracking()
                .Include(tf => tf.TicketNoNavigation)
                .Include(tf => tf.Flight)
                .FirstOrDefaultAsync(tf => tf.TicketNo == ticketNo && tf.FlightId == flightId);

            if (ticketFlight == null) return NotFound("Boleto no encontrado.");

            if (ticketFlight.FareConditions == "Business")
            {
                TempData["Error"] = "Este asiento ya es clase Business.";
                return RedirectToAction(nameof(BookingTickets), new { bookRef = ticketFlight.TicketNoNavigation.BookRef });
            }

            // Calculation Logic: Flat difference to Business
            // Economy -> Business = $200
            // Comfort -> Business = $100
            decimal upgradeCost = ticketFlight.FareConditions == "Economy" ? 200.00m : 100.00m;

            ViewData["TicketNo"] = ticketNo;
            ViewData["FlightId"] = flightId;
            ViewData["CurrentFare"] = ticketFlight.FareConditions;
            ViewData["UpgradeCost"] = upgradeCost;
            ViewData["BookRef"] = ticketFlight.TicketNoNavigation.BookRef;

            return View(ticketFlight);
        }

        // 4. Create Order and Pay (POST)
        [HttpPost]
        public async Task<IActionResult> CreateOrder(string ticketNo, int flightId, decimal upgradeCost)
        {
            var ticketFlight = await _context.TicketFlights
                .AsNoTracking()
                .Include(tf => tf.TicketNoNavigation)
                .FirstOrDefaultAsync(tf => tf.TicketNo == ticketNo && tf.FlightId == flightId);

            if (ticketFlight == null) return NotFound();

            // Validate cost on server
            decimal actualUpgradeCost = ticketFlight.FareConditions == "Economy" ? 200.00m : 100.00m;

            string bookRef = ticketFlight.TicketNoNavigation.BookRef;

            // Create Order
            var order = new Order
            {
                BookRef = bookRef,
                OrderDate = DateTime.UtcNow,
                TotalAmount = actualUpgradeCost,
                Status = "Pendiente"
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Create OrderDetails
            var detail = new OrderDetail
            {
                OrderId = order.OrderId,
                Description = $"Upgrade de Asiento a Business (Boleto #{ticketNo} / Vuelo {flightId})",
                Amount = actualUpgradeCost
            };
            _context.OrderDetails.Add(detail);
            await _context.SaveChangesAsync();

            string scheme = Request.Scheme;
            string host = Request.Host.Value ?? "localhost";
            string returnUrl = $"{scheme}://{host}/SeatUpgrade/PaymentSuccess";
            string cancelUrl = $"{scheme}://{host}/SeatUpgrade/PaymentCancel";

            var paypalResult = await _payPalService.CreateOrderAsync(
                actualUpgradeCost,
                $"Upgrade Boleto #{ticketNo}",
                returnUrl,
                cancelUrl
            );

            // Store transaction data in DB
            var payment = new Payment
            {
                OrderId = order.OrderId,
                PaymentDate = DateTime.UtcNow,
                Amount = actualUpgradeCost,
                Status = "Pendiente",
                Gateway = "PayPal",
                ExternalTransactionId = paypalResult.OrderId,
                Currency = "USD",
                UserId = User.Identity?.Name ?? "Usuario"
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Store context in TempData
            TempData["Upgrade_TicketNo"] = ticketNo;
            TempData["Upgrade_FlightId"] = flightId.ToString();
            TempData["Upgrade_OrderId"] = order.OrderId.ToString();
            TempData["Upgrade_PaymentId"] = payment.PaymentId.ToString();

            return Redirect(paypalResult.ApprovalUrl);
        }

        // 5. Success Webhook/Callback
        public async Task<IActionResult> PaymentSuccess(string token)
        {
            try
            {
                var captureResult = await _payPalService.CaptureOrderAsync(token);

                object? payIdObj = null;
                int paymentId = 0;
                bool hasSessionData = TempData.TryGetValue("Upgrade_PaymentId", out payIdObj) && 
                                      int.TryParse(payIdObj?.ToString(), out paymentId);

                if (captureResult.Status != "COMPLETED")
                {
                    if (hasSessionData)
                    {
                        var tempPayment = await _context.Payments
                            .Include(p => p.Order)
                            .FirstOrDefaultAsync(p => p.PaymentId == paymentId);
                        if (tempPayment != null)
                        {
                            string finalStatus = (captureResult.Status == "DECLINED" || captureResult.Status == "FAILED" || captureResult.Status == "DENIED") ? "Rechazado" : "Fallido";
                            tempPayment.Status = finalStatus;
                            tempPayment.Order.Status = finalStatus;
                            await _context.SaveChangesAsync();
                        }
                    }
                    TempData["Error"] = "El pago no pudo ser completado. Estado: " + captureResult.Status;
                    return RedirectToAction(nameof(Index));
                }

                if (!hasSessionData)
                {
                    return BadRequest("Datos de sesión perdidos.");
                }

                var payment = await _context.Payments
                    .Include(p => p.Order)
                    .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

                if (payment == null) return NotFound("Pago no encontrado en base de datos.");

                // Update Payment and Order statuses
                payment.Status = "Completado";
                payment.ResponseMessage = "PayPal Capture OK";
                payment.Order.Status = "Completed";

                // Execute the Business Logic (The actual Seat Upgrade)
                string ticketNo = TempData["Upgrade_TicketNo"]?.ToString() ?? "";
                string flightIdStr = TempData["Upgrade_FlightId"]?.ToString() ?? "";
                
                if (int.TryParse(flightIdStr, out int flightId))
                {
                    var ticketFlight = await _context.TicketFlights
                        .FirstOrDefaultAsync(tf => tf.TicketNo == ticketNo && tf.FlightId == flightId);
                        
                    if (ticketFlight != null)
                    {
                        // Upgrade fare class!
                        ticketFlight.FareConditions = "Business";
                        
                        // We could also record it in a history table if we had one.
                    }
                }

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Receipt), new { orderId = payment.OrderId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Ocurrió un error al procesar el pago: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> PaymentCancel()
        {
            if (TempData.TryGetValue("Upgrade_PaymentId", out var payIdObj) && 
                int.TryParse(payIdObj?.ToString(), out int paymentId))
            {
                var payment = await _context.Payments
                    .Include(p => p.Order)
                    .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

                if (payment != null)
                {
                    payment.Status = "Cancelado";
                    payment.Order.Status = "Cancelled";
                    await _context.SaveChangesAsync();
                }
            }

            TempData["Error"] = "Has cancelado el proceso de pago.";
            return RedirectToAction(nameof(Index));
        }

        // 6. Receipt
        public async Task<IActionResult> Receipt(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return NotFound();

            return View(order);
        }
    }
}
