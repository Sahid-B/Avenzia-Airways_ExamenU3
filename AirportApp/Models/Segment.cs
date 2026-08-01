using System;
using System.Collections.Generic;

namespace AirportApp.Models;

/// <summary>
/// Flight segment (leg)
/// </summary>
public partial class Segment
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
    /// Travel class
    /// </summary>
    public string FareConditions { get; set; } = null!;

    /// <summary>
    /// Travel price
    /// </summary>
    public decimal Price { get; set; }

    public virtual BoardingPass? BoardingPass { get; set; }

    public virtual Flight Flight { get; set; } = null!;

    public virtual Ticket TicketNoNavigation { get; set; } = null!;
}
