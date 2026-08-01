using System;
using System.Collections.Generic;

namespace AirportApp.Models;

/// <summary>
/// Tickets
/// </summary>
public partial class Ticket
{
    /// <summary>
    /// Ticket number
    /// </summary>
    public string TicketNo { get; set; } = null!;

    /// <summary>
    /// Booking number
    /// </summary>
    public string BookRef { get; set; } = null!;

    /// <summary>
    /// Passenger ID
    /// </summary>
    public string PassengerId { get; set; } = null!;

    /// <summary>
    /// Passenger name
    /// </summary>
    public string PassengerName { get; set; } = null!;

    /// <summary>
    /// Outbound flight?
    /// </summary>
    public bool Outbound { get; set; }

    public virtual Booking BookRefNavigation { get; set; } = null!;

    public virtual ICollection<Segment> Segments { get; set; } = new List<Segment>();

    public virtual ICollection<TicketFlight> TicketFlights { get; set; } = new List<TicketFlight>();
}
