using System;
using System.Collections.Generic;
using NpgsqlTypes;

namespace AirportApp.Models;

/// <summary>
/// Routes
/// </summary>
public partial class Route
{
    /// <summary>
    /// Route number
    /// </summary>
    public string RouteNo { get; set; } = null!;

    /// <summary>
    /// Period of validity
    /// </summary>
    public NpgsqlRange<DateTime> Validity { get; set; }

    /// <summary>
    /// Airport of departure
    /// </summary>
    public string DepartureAirport { get; set; } = null!;

    /// <summary>
    /// Airport of arrival
    /// </summary>
    public string ArrivalAirport { get; set; } = null!;

    /// <summary>
    /// Airplane code, IATA
    /// </summary>
    public string AirplaneCode { get; set; } = null!;

    /// <summary>
    /// Days of week array
    /// </summary>
    public List<int> DaysOfWeek { get; set; } = null!;

    /// <summary>
    /// Scheduled local time of departure
    /// </summary>
    public TimeOnly ScheduledTime { get; set; }

    /// <summary>
    /// Estimated duration
    /// </summary>
    public TimeSpan Duration { get; set; }

    public virtual AirplanesDatum AirplaneCodeNavigation { get; set; } = null!;

    public virtual AirportsDatum ArrivalAirportNavigation { get; set; } = null!;

    public virtual AirportsDatum DepartureAirportNavigation { get; set; } = null!;
}
