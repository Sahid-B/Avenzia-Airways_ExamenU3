using System;
using System.Collections.Generic;
using NpgsqlTypes;

namespace AirportApp.Models;

/// <summary>
/// Airports (internal multilingual data)
/// </summary>
public partial class AirportsDatum
{
    /// <summary>
    /// Airport code, IATA
    /// </summary>
    public string AirportCode { get; set; } = null!;

    /// <summary>
    /// Airport name
    /// </summary>
    public string AirportName { get; set; } = null!;

    /// <summary>
    /// City
    /// </summary>
    public string City { get; set; } = null!;

    /// <summary>
    /// Country
    /// </summary>
    public string Country { get; set; } = null!;

    /// <summary>
    /// Airport coordinates (longitude and latitude)
    /// </summary>
    public NpgsqlPoint Coordinates { get; set; }

    /// <summary>
    /// Airport time zone
    /// </summary>
    public string Timezone { get; set; } = null!;
}
