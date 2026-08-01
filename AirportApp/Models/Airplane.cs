using System;
using System.Collections.Generic;

namespace AirportApp.Models;

public partial class Airplane
{
    /// <summary>
    /// Airplane code, IATA
    /// </summary>
    public string? AirplaneCode { get; set; }

    /// <summary>
    /// Airplane model
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Maximum flight range, km
    /// </summary>
    public int? Range { get; set; }

    /// <summary>
    /// Cruise speed, km/h
    /// </summary>
    public int? Speed { get; set; }
}
