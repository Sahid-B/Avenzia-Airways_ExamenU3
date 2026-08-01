using System;
using System.Collections.Generic;

namespace AirportApp.Models;

/// <summary>
/// Bookings
/// </summary>
public partial class Booking
{
    /// <summary>
    /// Booking number
    /// </summary>
    public string BookRef { get; set; } = null!;

    /// <summary>
    /// Booking date
    /// </summary>
    public DateTime BookDate { get; set; }

    /// <summary>
    /// Total booking amount
    /// </summary>
    public decimal TotalAmount { get; set; }

    public virtual ICollection<FlightChangeHistory> FlightChangeHistories { get; set; } = new List<FlightChangeHistory>();

    public virtual ICollection<FlightChangeRequest> FlightChangeRequests { get; set; } = new List<FlightChangeRequest>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual ICollection<TransactionHistory> TransactionHistories { get; set; } = new List<TransactionHistory>();
}
