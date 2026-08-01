using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AirportApp.Models;
using AirportApp.Services.Payments;
using System.Security.Claims;
using System.Text;

namespace AirportApp.Controllers
{
    [Authorize]
    public class ShoppingController : Controller
    {
        private readonly AirportDbContext _context;
        private readonly PayPalService _payPalService;

        private readonly PayPhoneApiLinkService _payPhoneService;

        public ShoppingController(AirportDbContext context, PayPalService payPalService, PayPhoneApiLinkService payPhoneService)
        {
            _context = context;
            _payPalService = payPalService;
            _payPhoneService = payPhoneService;
        }

        // GET: Shopping/Index
        public async Task<IActionResult> Index(int? pageNumber, int? pageSize)
        {
            int actualPageSize = pageSize ?? 10;
            var flightsQuery = _context.Timetables
                .Where(f => f.Status == "Scheduled")
                .OrderByDescending(f => f.ScheduledDeparture)
                .AsNoTracking();

            return View(await PaginatedList<Timetable>.CreateAsync(flightsQuery, pageNumber ?? 1, actualPageSize));
        }

        // GET: Shopping/SelectFlight/5
        public async Task<IActionResult> SelectFlight(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var flight = await _context.Flights
                .Include(f => f.Segments)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.FlightId == id);

            if (flight == null)
            {
                return NotFound();
            }

            return View(flight);
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(int FlightId, string FareCondition, string provider)
        {
            var flight = await _context.Flights.FindAsync(FlightId);
            if (flight == null) return NotFound();

            decimal price = FareCondition switch
            {
                "Economy" => 150.00m,
                "Comfort" => 250.00m,
                "Business" => 350.00m,
                _ => 150.00m
            };

            // 1. Create Booking
            string bookRef = GenerateBookRef();
            var booking = new Booking
            {
                BookRef = bookRef,
                BookDate = DateTime.UtcNow,
                TotalAmount = price
            };
            _context.Bookings.Add(booking);

            // 2. Create Ticket
            var ticket = new Ticket
            {
                TicketNo = GenerateTicketNo(),
                BookRef = bookRef,
                PassengerId = User.FindFirstValue(ClaimTypes.NameIdentifier)?.Substring(0, Math.Min(20, User.FindFirstValue(ClaimTypes.NameIdentifier)?.Length ?? 0)) ?? "USR01",
                PassengerName = User.Identity?.Name ?? "Unknown Passenger",
                Outbound = true
            };
            _context.Tickets.Add(ticket);

            // 3. Create TicketFlight
            var ticketFlight = new TicketFlight
            {
                TicketNo = ticket.TicketNo,
                FlightId = FlightId,
                FareConditions = FareCondition,
                Amount = price
            };
            _context.TicketFlights.Add(ticketFlight);

            // 4. Create Order
            var order = new Order
            {
                BookRef = bookRef,
                OrderDate = DateTime.UtcNow,
                TotalAmount = price,
                Status = "Pending"
            };
            _context.Orders.Add(order);

            await _context.SaveChangesAsync();

            // 5. Create OrderDetail
            var orderDetail = new OrderDetail
            {
                OrderId = order.OrderId,
                Description = $"Vuelo {flight.RouteNo} - Clase {FareCondition}",
                Amount = price
            };
            _context.OrderDetails.Add(orderDetail);
            await _context.SaveChangesAsync();

            string externalId = string.Empty;
            string paymentUrl = string.Empty;

            if (provider == "PayPhone")
            {
                // PayPhone Link integration
                string clientTransactionId = DateTime.Now.ToString("yyMMddHHmmssfff")[..15];
                string reference = $"Compra #{order.OrderId}";
                
                paymentUrl = await _payPhoneService.CreatePaymentLinkAsync(price, clientTransactionId, reference);
                externalId = clientTransactionId;
            }
            else
            {
                // PayPal (Standard or Embedded)
                string returnUrl = Url.Action("PaymentSuccess", "Shopping", new { orderId = order.OrderId }, Request.Scheme) ?? "";
                string cancelUrl = Url.Action("PaymentCancel", "Shopping", new { orderId = order.OrderId }, Request.Scheme) ?? "";

                try
                {
                    var payPalResult = await _payPalService.CreateOrderAsync(price, order.OrderId.ToString(), returnUrl, cancelUrl);
                    if (!string.IsNullOrEmpty(payPalResult.ApprovalUrl))
                    {
                        paymentUrl = payPalResult.ApprovalUrl;
                        externalId = payPalResult.OrderId; // PayPal Order ID
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al conectar con PayPal: " + ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            // Create Payment record
            var payment = new Payment
            {
                OrderId = order.OrderId,
                PaymentDate = DateTime.UtcNow,
                Amount = price,
                Status = "Pendiente",
                Gateway = provider,
                ExternalTransactionId = externalId,
                Currency = "USD",
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                ResponseMessage = paymentUrl
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

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

        public async Task<IActionResult> PaymentSuccess(int orderId, string token)
        {
            var order = await _context.Orders
                .Include(o => o.BookRefNavigation)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return NotFound();

            string finalStatus = "Failed";
            try
            {
                var captureResult = await _payPalService.CaptureOrderAsync(token);

                if (captureResult.Status == "COMPLETED")
                {
                    order.Status = "Completed";
                    
                    var payment = new Payment
                    {
                        OrderId = order.OrderId,
                        PaymentDate = DateTime.UtcNow,
                        Amount = order.TotalAmount,
                        Status = "Completed",
                        UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                        Gateway = "PayPal",
                        ExternalTransactionId = captureResult.CaptureId,
                        Currency = "USD",
                        ConfirmationDate = DateTime.UtcNow,
                        ResponseMessage = "Pago exitoso"
                    };

                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Receipt), new { orderId = order.OrderId });
                }
                else if (captureResult.Status == "DECLINED" || captureResult.Status == "FAILED" || captureResult.Status == "DENIED")
                {
                    finalStatus = "Rechazado";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al capturar el pago: " + ex.Message;
            }

            order.Status = finalStatus;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> PaymentCancel(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = "Cancelled";
                await _context.SaveChangesAsync();
            }

            TempData["Error"] = "El pago ha sido cancelado.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Receipt(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null || order.Status != "Completed")
            {
                return NotFound();
            }

            return View(order);
        }

        private string GenerateBookRef()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string GenerateTicketNo()
        {
            var random = new Random();
            return random.Next(100000000, 999999999).ToString("D13");
        }
    }
}
