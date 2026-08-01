using System;
using System.Collections.Generic;

namespace AirportApp.Models;

public partial class OrderDetail
{
    public int OrderDetailId { get; set; }

    public int OrderId { get; set; }

    public string Description { get; set; } = null!;

    public decimal Amount { get; set; }

    public virtual Order Order { get; set; } = null!;
}
