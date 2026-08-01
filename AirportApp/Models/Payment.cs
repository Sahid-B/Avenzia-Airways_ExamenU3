using System;
using System.Collections.Generic;

namespace AirportApp.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int OrderId { get; set; }

    public DateTime? PaymentDate { get; set; }

    public decimal Amount { get; set; }

    public string? Status { get; set; }

    public string? UserId { get; set; }

    public string? Gateway { get; set; }

    public string? ExternalTransactionId { get; set; }

    public string? Currency { get; set; }

    public DateTime? ConfirmationDate { get; set; }

    public string? ResponseMessage { get; set; }

    public virtual Order Order { get; set; } = null!;
}
