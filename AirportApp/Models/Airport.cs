using System;
using System.Collections.Generic;
using NpgsqlTypes;

namespace AirportApp.Models;

public partial class Airport
{
    /// <summary>
    /// Airport code, IATA
    /// </summary>
    public string? AirportCode { get; set; }

    /// <summary>
    /// Airport name
    /// </summary>
    public string? AirportName { get; set; }

    /// <summary>
    /// City
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Country
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Airport coordinates (longitude and latitude)
    /// </summary>
    public NpgsqlPoint? Coordinates { get; set; }

    /// <summary>
    /// Airport time zone
    /// </summary>
    public string? Timezone { get; set; }
}
