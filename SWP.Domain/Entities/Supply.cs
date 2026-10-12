using System;
using System.Collections.Generic;

namespace SWP.Domain.Models.Entity;

public class Supply
{
    public int SupplyId { get; set; }

    public int? UserId { get; set; }

    public string SupplyName { get; set; } = null!;

    public string? Category { get; set; }

    public string? Description { get; set; }

    public decimal QuantityInStock { get; set; }

    public string? Unit { get; set; }

    public decimal? UnitPrice { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public string? StorageLocation { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? User { get; set; }
}
