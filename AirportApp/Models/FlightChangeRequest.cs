using System;
using System.Collections.Generic;

namespace AirportApp.Models;

public partial class FlightChangeRequest
{
    public int RequestId { get; set; }

    public string BookRef { get; set; } = null!;

    public int RequestedFlightId { get; set; }

    public DateTime? RequestDate { get; set; }

    public string? Status { get; set; }

    public virtual Booking BookRefNavigation { get; set; } = null!;

    public virtual Flight RequestedFlight { get; set; } = null!;
}
