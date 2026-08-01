using System;
using System.Collections.Generic;

namespace AirportApp.Models;

/// <summary>
/// Seats
/// </summary>
public partial class Seat
{
    /// <summary>
    /// Airplane code, IATA
    /// </summary>
    public string AirplaneCode { get; set; } = null!;

    /// <summary>
    /// Seat number
    /// </summary>
    public string SeatNo { get; set; } = null!;

    /// <summary>
    /// Travel class
    /// </summary>
    public string FareConditions { get; set; } = null!;

    public virtual AirplanesDatum AirplaneCodeNavigation { get; set; } = null!;
}
