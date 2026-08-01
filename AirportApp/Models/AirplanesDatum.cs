using System;
using System.Collections.Generic;

namespace AirportApp.Models;

/// <summary>
/// Airplanes (internal multilingual data)
/// </summary>
public partial class AirplanesDatum
{
    /// <summary>
    /// Airplane code, IATA
    /// </summary>
    public string AirplaneCode { get; set; } = null!;

    /// <summary>
    /// Airplane model
    /// </summary>
    public string Model { get; set; } = null!;

    /// <summary>
    /// Maximum flight range, km
    /// </summary>
    public int Range { get; set; }

    /// <summary>
    /// Cruise speed, km/h
    /// </summary>
    public int Speed { get; set; }

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
