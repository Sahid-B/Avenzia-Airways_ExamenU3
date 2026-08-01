using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AirportApp.Filters
{
    public class AdminOnlyMutationsFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var actionName = context.RouteData.Values["action"]?.ToString();
            var controllerName = context.RouteData.Values["controller"]?.ToString();

            // All 10 main CRUD controllers
            var crudControllers = new[] 
            { 
                "AirportsData", "Bookings", "FlightChangeHistories", "FlightChangeRequests", 
                "Flights", "OrderDetails", "Orders", "Payments", "TicketFlights", "Tickets" 
            };

            if (crudControllers.Contains(controllerName, StringComparer.OrdinalIgnoreCase))
            {
                var actionLower = actionName?.ToLowerInvariant();
                if (actionLower == "create" || actionLower == "edit" || actionLower == "delete" || actionLower == "deleteconfirmed")
                {
                    var user = context.HttpContext.User;
                    if (user == null || !user.Identity.IsAuthenticated || !user.IsInRole("Administrador"))
                    {
                        // Return HTTP 403 Forbidden
                        context.Result = new ForbidResult();
                        return;
                    }
                }
            }

            await next();
        }
    }
}
