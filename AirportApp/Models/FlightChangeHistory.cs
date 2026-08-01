using System;
using System.Collections.Generic;

namespace AirportApp.Models;

public partial class FlightChangeHistory
{
    public int ChangeId { get; set; }

    public string BookRef { get; set; } = null!;

    public int OldFlightId { get; set; }

    public int NewFlightId { get; set; }

    public DateTime? ChangeDate { get; set; }

    public string? Reason { get; set; }

    public virtual Booking BookRefNavigation { get; set; } = null!;

    public virtual Flight NewFlight { get; set; } = null!;

    public virtual Flight OldFlight { get; set; } = null!;
}
