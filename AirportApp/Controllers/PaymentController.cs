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
    public class PaymentController : Controller
    {
        private readonly AirportDbContext _context;
        private readonly PayPalService _payPalService;
        private readonly AirportApp.Settings.PayPhoneSettings _payPhoneSettings;
        private readonly AirportApp.Settings.PayPalSettings _payPalSettings;

        public PaymentController(
            AirportDbContext context, 
            PayPalService payPalService, 
            Microsoft.Extensions.Options.IOptions<AirportApp.Settings.PayPhoneSettings> payPhoneOptions,
            Microsoft.Extensions.Options.IOptions<AirportApp.Settings.PayPalSettings> payPalOptions)
        {
            _context = context;
            _payPalService = payPalService;
            _payPhoneSettings = payPhoneOptions.Value;
            _payPalSettings = payPalOptions.Value;
        }

        // GET: Payment/PayPalEmbed
        [HttpGet]
        public async Task<IActionResult> PayPalEmbed(int paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .ThenInclude(o => o.OrderDetails)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null) return NotFound("Pago no encontrado.");

            ViewData["PayPalClientId"] = _payPalSettings.ClientId;

            return View(payment);
        }

        // POST: Payment/ConfirmPayPal
        [HttpPost]
        public async Task<IActionResult> ConfirmPayPal(int paymentId, string? orderId)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null) return NotFound();

            if (payment.Status == "Aprobado")
            {
                return RedirectToAction(nameof(Receipt), new { id = payment.PaymentId });
            }

            payment.Status = "Aprobado";
            payment.Order.Status = "Aprobado";
            payment.ConfirmationDate = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(orderId))
            {
                payment.ExternalTransactionId = orderId;
            }

            // Retrieve wizard selections from TempData
            string? ticketNo = TempData["Reschedule_TicketNo"] as string;
            string? oldFlightIdStr = TempData["Reschedule_OldFlightId"] as string;
            string? newFlightIdStr = TempData["Reschedule_NewFlightId"] as string;
            string? requestIdStr = TempData["Reschedule_RequestId"] as string;

            if (!string.IsNullOrEmpty(ticketNo) && 
                int.TryParse(oldFlightIdStr, out int oldFlightId) && 
                int.TryParse(newFlightIdStr, out int newFlightId))
            {
                var oldTicketFlight = await _context.TicketFlights
                    .FirstOrDefaultAsync(tf => tf.TicketNo == ticketNo && tf.FlightId == oldFlightId);

                if (oldTicketFlight != null)
                {
                    decimal newPrice = await GetBasePriceForFlightAsync(newFlightId);
                    
                    _context.TicketFlights.Remove(oldTicketFlight);
                    await _context.SaveChangesAsync();

                    var newTicketFlight = new TicketFlight
                    {
                        TicketNo = ticketNo,
                        FlightId = newFlightId,
                        FareConditions = oldTicketFlight.FareConditions,
                        Amount = newPrice
                    };
                    _context.TicketFlights.Add(newTicketFlight);

                    var history = new FlightChangeHistory
                    {
                        BookRef = payment.Order.BookRef,
                        OldFlightId = oldFlightId,
                        NewFlightId = newFlightId,
                        ChangeDate = DateTime.UtcNow,
                        Reason = $"Reprogramación aprobada. PayPal Embebido: {payment.ExternalTransactionId}"
                    };
                    _context.FlightChangeHistories.Add(history);
                }

                if (int.TryParse(requestIdStr, out int requestId))
                {
                    var req = await _context.FlightChangeRequests.FindAsync(requestId);
                    if (req != null)
                    {
                        req.Status = "Aprobado";
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Receipt), new { id = payment.PaymentId });
        }

        // PayPal Success callback: /Payment/Success?token=xxx
        public async Task<IActionResult> Success(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("No se proporcionó el token de PayPal.");
            }

            // 1. Find the payment transaction
            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Gateway == "PayPal" && p.ExternalTransactionId == token);

            if (payment == null)
            {
                return NotFound("Transacción de pago no encontrada.");
            }

            // Prevent double-processing: "La aplicación deberá impedir que una misma transacción sea registrada dos veces."
            if (payment.Status == "Aprobado")
            {
                return RedirectToAction(nameof(Receipt), new { id = payment.PaymentId });
            }

            // 2. Capture the PayPal payment
            var captureResult = await _payPalService.CaptureOrderAsync(token);
            payment.ResponseMessage = captureResult.RawResponse;
            payment.ConfirmationDate = DateTime.UtcNow;

            if (captureResult.Status == "COMPLETED")
            {
                payment.Status = "Aprobado";
                payment.Order.Status = "Aprobado";

                // Retrieve wizard selections from TempData
                string? ticketNo = TempData["Reschedule_TicketNo"] as string;
                string? oldFlightIdStr = TempData["Reschedule_OldFlightId"] as string;
                string? newFlightIdStr = TempData["Reschedule_NewFlightId"] as string;
                string? requestIdStr = TempData["Reschedule_RequestId"] as string;

                if (!string.IsNullOrEmpty(ticketNo) && 
                    int.TryParse(oldFlightIdStr, out int oldFlightId) && 
                    int.TryParse(newFlightIdStr, out int newFlightId))
                {
                    // Reprogram flight physically in database (delete & insert because FlightId is a composite PK)
                    var oldTicketFlight = await _context.TicketFlights
                        .FirstOrDefaultAsync(tf => tf.TicketNo == ticketNo && tf.FlightId == oldFlightId);

                    if (oldTicketFlight != null)
                    {
                        // Calculate price for the new flight
                        decimal newPrice = await GetBasePriceForFlightAsync(newFlightId);
                        
                        _context.TicketFlights.Remove(oldTicketFlight);
                        await _context.SaveChangesAsync();

                        var newTicketFlight = new TicketFlight
                        {
                            TicketNo = ticketNo,
                            FlightId = newFlightId,
                            FareConditions = oldTicketFlight.FareConditions,
                            Amount = newPrice
                        };
                        _context.TicketFlights.Add(newTicketFlight);

                        // Register the flight change history
                        var history = new FlightChangeHistory
                        {
                            BookRef = payment.Order.BookRef,
                            OldFlightId = oldFlightId,
                            NewFlightId = newFlightId,
                            ChangeDate = DateTime.UtcNow,
                            Reason = $"Reprogramación aprobada. Pago PayPal: {token}"
                        };
                        _context.FlightChangeHistories.Add(history);
                    }

                    // Update Change Request status
                    if (int.TryParse(requestIdStr, out int requestId))
                    {
                        var req = await _context.FlightChangeRequests.FindAsync(requestId);
                        if (req != null)
                        {
                            req.Status = "Aprobado";
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Receipt), new { id = payment.PaymentId });
            }
            else
            {
                payment.Status = "Fallido";
                payment.Order.Status = "Fallido";
                await _context.SaveChangesAsync();
                
                ViewData["ErrorMessage"] = $"El pago de PayPal no se completó (Estado: {captureResult.Status}).";
                return View("Cancel");
            }
        }

        // PayPal Cancel callback: /Payment/Cancel?token=xxx
        public async Task<IActionResult> Cancel(string token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                var payment = await _context.Payments
                    .Include(p => p.Order)
                    .FirstOrDefaultAsync(p => p.Gateway == "PayPal" && p.ExternalTransactionId == token);

                if (payment != null && payment.Status == "Pendiente")
                {
                    payment.Status = "Cancelado";
                    payment.Order.Status = "Cancelado";

                    string? requestIdStr = TempData["Reschedule_RequestId"] as string;
                    if (int.TryParse(requestIdStr, out int requestId))
                    {
                        var req = await _context.FlightChangeRequests.FindAsync(requestId);
                        if (req != null)
                        {
                            req.Status = "Cancelado";
                        }
                    }

                    await _context.SaveChangesAsync();
                }
            }

            ViewData["ErrorMessage"] = "El proceso de pago fue cancelado por el usuario.";
            return View();
        }

        // PayPhone checkout intermediate page
        [HttpGet]
        public async Task<IActionResult> PayPhoneCheckout(int paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .ThenInclude(o => o.OrderDetails)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null) return NotFound("Pago no encontrado.");
            return View(payment);
        }

        // Simulate PayPhone Payment success: /Payment/ConfirmPayPhone
        [HttpPost]
        public async Task<IActionResult> ConfirmPayPhone(int paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null) return NotFound();

            if (payment.Status == "Aprobado")
            {
                return RedirectToAction(nameof(Receipt), new { id = payment.PaymentId });
            }

            payment.Status = "Aprobado";
            payment.Order.Status = "Aprobado";
            payment.ConfirmationDate = DateTime.UtcNow;

            // Retrieve wizard selections from TempData
            string? ticketNo = TempData["Reschedule_TicketNo"] as string;
            string? oldFlightIdStr = TempData["Reschedule_OldFlightId"] as string;
            string? newFlightIdStr = TempData["Reschedule_NewFlightId"] as string;
            string? requestIdStr = TempData["Reschedule_RequestId"] as string;

            if (!string.IsNullOrEmpty(ticketNo) && 
                int.TryParse(oldFlightIdStr, out int oldFlightId) && 
                int.TryParse(newFlightIdStr, out int newFlightId))
            {
                var oldTicketFlight = await _context.TicketFlights
                    .FirstOrDefaultAsync(tf => tf.TicketNo == ticketNo && tf.FlightId == oldFlightId);

                if (oldTicketFlight != null)
                {
                    decimal newPrice = await GetBasePriceForFlightAsync(newFlightId);
                    
                    _context.TicketFlights.Remove(oldTicketFlight);
                    await _context.SaveChangesAsync();

                    var newTicketFlight = new TicketFlight
                    {
                        TicketNo = ticketNo,
                        FlightId = newFlightId,
                        FareConditions = oldTicketFlight.FareConditions,
                        Amount = newPrice
                    };
                    _context.TicketFlights.Add(newTicketFlight);

                    var history = new FlightChangeHistory
                    {
                        BookRef = payment.Order.BookRef,
                        OldFlightId = oldFlightId,
                        NewFlightId = newFlightId,
                        ChangeDate = DateTime.UtcNow,
                        Reason = $"Reprogramación aprobada. Pago PayPhone: {payment.ExternalTransactionId}"
                    };
                    _context.FlightChangeHistories.Add(history);
                }

                if (int.TryParse(requestIdStr, out int requestId))
                {
                    var req = await _context.FlightChangeRequests.FindAsync(requestId);
                    if (req != null)
                    {
                        req.Status = "Aprobado";
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Receipt), new { id = payment.PaymentId });
        }

        // GET: Receipt
        [HttpGet]
        public async Task<IActionResult> Receipt(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .ThenInclude(o => o.OrderDetails)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null) return NotFound();
            return View(payment);
        }

        private async Task<decimal> GetBasePriceForFlightAsync(int flightId)
        {
            var avg = await _context.TicketFlights
                .Where(tf => tf.FlightId == flightId)
                .Select(tf => (decimal?)tf.Amount)
                .AverageAsync();

            return avg ?? 250.00m;
        }
    }
}
