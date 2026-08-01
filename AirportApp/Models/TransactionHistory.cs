using System;
using System.Collections.Generic;

namespace AirportApp.Models;

public partial class TransactionHistory
{
    public int TransactionId { get; set; }

    public string BookRef { get; set; } = null!;

    public string? UserId { get; set; }

    public string TransactionType { get; set; } = null!;

    public DateTime? TransactionDate { get; set; }

    public string? Details { get; set; }

    public virtual Booking BookRefNavigation { get; set; } = null!;
}
