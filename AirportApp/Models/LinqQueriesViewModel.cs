using System;
using System.Collections.Generic;

namespace AirportApp.Models
{
    public class LinqQueriesViewModel
    {
        // 1. Projection (Select)
        public class BookingProjection
        {
            public string BookRef { get; set; } = string.Empty;
            public DateTime BookDate { get; set; }
            public decimal TotalAmount { get; set; }
            public int TicketsCount { get; set; } // Uses Include
        }

        // 2. Aggregation and Grouping (GroupBy, Count, Sum, Average)
        public class PaymentStats
        {
            public string Status { get; set; } = string.Empty;
            public int Count { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal AverageAmount { get; set; }
        }

        // 3. Simple OrderBy & Projection
        public class AirportProjection
        {
            public string AirportCode { get; set; } = string.Empty;
            public string AirportName { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
        }

        // --- Data Properties ---
        public PaginatedList<BookingProjection>? Bookings { get; set; } // Query 1
        public List<PaymentStats> PaymentStatistics { get; set; } = new(); // Query 2
        public List<AirportProjection> TopAirports { get; set; } = new(); // Query 3
        public bool HasPendingPayments { get; set; } // Query 4

        // --- Form Fields for Query 1 ---
        public string? SearchBookRef { get; set; }
        public DateTime? FilterDate { get; set; }
        public decimal? MinAmount { get; set; }
    }
}
