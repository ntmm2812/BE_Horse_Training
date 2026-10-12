using System;
using System.Collections.Generic;

namespace SWP.Domain.Models.Entity;

public class FinancialTransaction
{
    public int TransactionId { get; set; }

    public int UserId { get; set; }

    public string TransactionCode { get; set; } = null!;

    public string TransactionType { get; set; } = null!;

    public string? Category { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = null!;

    public DateTime Date { get; set; }

    public string? PaymentMethod { get; set; }

    public string Status { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
