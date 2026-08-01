using System;
using System.Collections.Generic;

namespace AirportApp.Models;

/// <summary>
/// Flights
/// </summary>
public partial class Flight
{
    /// <summary>
    /// Flight ID
    /// </summary>
    public int FlightId { get; set; }

    /// <summary>
    /// Route number
    /// </summary>
    public string RouteNo { get; set; } = null!;

    /// <summary>
    /// Flight status
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Scheduled departure time
    /// </summary>
    public DateTime ScheduledDeparture { get; set; }

    /// <summary>
    /// Scheduled arrival time
    /// </summary>
    public DateTime ScheduledArrival { get; set; }

    /// <summary>
    /// Actual departure time
    /// </summary>
    public DateTime? ActualDeparture { get; set; }

    /// <summary>
    /// Actual arrival time
    /// </summary>
    public DateTime? ActualArrival { get; set; }

    public virtual ICollection<FlightChangeHistory> FlightChangeHistoryNewFlights { get; set; } = new List<FlightChangeHistory>();

    public virtual ICollection<FlightChangeHistory> FlightChangeHistoryOldFlights { get; set; } = new List<FlightChangeHistory>();

    public virtual ICollection<FlightChangeRequest> FlightChangeRequests { get; set; } = new List<FlightChangeRequest>();

    public virtual ICollection<Segment> Segments { get; set; } = new List<Segment>();

    public virtual ICollection<TicketFlight> TicketFlights { get; set; } = new List<TicketFlight>();
}
