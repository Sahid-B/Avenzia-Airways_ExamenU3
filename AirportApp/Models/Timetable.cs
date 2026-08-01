using System;
using System.Collections.Generic;

namespace AirportApp.Models;

public partial class Timetable
{
    /// <summary>
    /// Flight ID
    /// </summary>
    public int? FlightId { get; set; }

    /// <summary>
    /// Route number
    /// </summary>
    public string? RouteNo { get; set; }

    /// <summary>
    /// Airport of departure
    /// </summary>
    public string? DepartureAirport { get; set; }

    /// <summary>
    /// Airport of arrival
    /// </summary>
    public string? ArrivalAirport { get; set; }

    /// <summary>
    /// Flight status
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Airplane code, IATA
    /// </summary>
    public string? AirplaneCode { get; set; }

    /// <summary>
    /// Scheduled departure time
    /// </summary>
    public DateTime? ScheduledDeparture { get; set; }

    /// <summary>
    /// Scheduled departure time in airport&apos;s timezone
    /// </summary>
    public DateTime? ScheduledDepartureLocal { get; set; }

    /// <summary>
    /// Actual departure time
    /// </summary>
    public DateTime? ActualDeparture { get; set; }

    /// <summary>
    /// Actual departure time in airport&apos;s timezone
    /// </summary>
    public DateTime? ActualDepartureLocal { get; set; }

    /// <summary>
    /// Scheduled arrival time
    /// </summary>
    public DateTime? ScheduledArrival { get; set; }

    /// <summary>
    /// Scheduled arrival time in airport&apos;s timezone
    /// </summary>
    public DateTime? ScheduledArrivalLocal { get; set; }

    /// <summary>
    /// Actual arrival time
    /// </summary>
    public DateTime? ActualArrival { get; set; }

    /// <summary>
    /// Actual arrival time in airport&apos;s timezone
    /// </summary>
    public DateTime? ActualArrivalLocal { get; set; }
}
