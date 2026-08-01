using System;
using System.Collections.Generic;

namespace AirportApp.Models;

/// <summary>
/// Boarding passes
/// </summary>
public partial class BoardingPass
{
    /// <summary>
    /// Ticket number
    /// </summary>
    public string TicketNo { get; set; } = null!;

    /// <summary>
    /// Flight ID
    /// </summary>
    public int FlightId { get; set; }

    /// <summary>
    /// Seat number
    /// </summary>
    public string SeatNo { get; set; } = null!;

    /// <summary>
    /// Boarding pass number
    /// </summary>
    public int? BoardingNo { get; set; }

    /// <summary>
    /// Boarding time
    /// </summary>
    public DateTime? BoardingTime { get; set; }

    public virtual Segment Segment { get; set; } = null!;
}
